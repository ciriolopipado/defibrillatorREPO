using System;
using System.Collections.Generic;

namespace Defibrillator.Core;

public enum Outcome
{
    Charging,
    Revived,
    Cancelled,
    InvalidRequest,
    InvalidTarget,
    ItemUnavailable,
    Busy,
    EmptyBattery,
    Interrupted,
    TimedOut,
    RevivalFailed
}

public readonly struct Result
{
    public Outcome Outcome { get; }
    public double Progress { get; }
    public int Charge { get; }

    internal Result(Outcome outcome, double progress, int charge)
    {
        Outcome = outcome;
        Progress = progress;
        Charge = charge;
    }
}

/// <summary>
/// Host-only state machine. Call only from the Unity main thread. It accepts authenticated
/// actor IDs, not claimed identities. Time, world state, charging and item registration
/// must be supplied by the trusted game adapter. No Unity or networking APIs are used here.
/// </summary>
public sealed class RevivalService
{
    private sealed class ItemState
    {
        public int Charge;
        public Session? Session;
        public ItemState(int charge) => Charge = charge;
    }

    private sealed class Session
    {
        public int Actor;
        public int Target;
        public string Life = "";
        public long Damage;
        public double Started;
        public double LastHeartbeat;
    }

    private readonly Settings settings;
    private readonly IRevivalWorld world;
    private readonly Func<double> clock;
    private readonly Dictionary<string, ItemState> items = new Dictionary<string, ItemState>();
    private readonly HashSet<string> completedLives = new HashSet<string>();
    private double lastTime = double.NegativeInfinity;
    private bool inCommit;

    public RevivalService(Settings settings, IRevivalWorld world, Func<double> monotonicClock)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.world = world ?? throw new ArgumentNullException(nameof(world));
        clock = monotonicClock ?? throw new ArgumentNullException(nameof(monotonicClock));
    }

    /// <summary>Register a host-observed physical item. Duplicate registration cannot refill it.</summary>
    public void RegisterItem(string itemId, int charge = 100)
    {
        EnsureNotCommitting();
        ValidateItemId(itemId);
        ValidateCharge(charge);
        if (items.ContainsKey(itemId)) throw new InvalidOperationException("Item already registered.");
        items.Add(itemId, new ItemState(charge));
    }

    public void RemoveItem(string itemId)
    {
        EnsureNotCommitting();
        items.Remove(itemId);
    }

    public int GetCharge(string itemId) => items.TryGetValue(itemId, out var item)
        ? item.Charge : throw new ArgumentException("Unknown item.", nameof(itemId));

    /// <summary>
    /// Host-observed charger update only; not a client request handler. Battery authority
    /// must remain in the adapter. Cancel any ongoing use before applying a charge change.
    /// </summary>
    public void UpdateChargeFromCharger(string itemId, int charge)
    {
        EnsureNotCommitting();
        ValidateCharge(charge);
        if (!items.TryGetValue(itemId, out var item)) throw new ArgumentException("Unknown item.");
        item.Session = null;
        item.Charge = charge;
    }

    /// <summary>
    /// Begin or continue holding. A timeout never completes an old attempt: a fresh
    /// attempt must start from zero. The adapter should send holds every 0.1–0.25 seconds.
    /// </summary>
    public Result Hold(int authenticatedActor, string itemId, int targetActor)
    {
        EnsureNotCommitting();
        double now = ReadTime();
        Expire(now);
        if (authenticatedActor <= 0 || targetActor <= 0 || authenticatedActor == targetActor ||
            string.IsNullOrWhiteSpace(itemId)) return new Result(Outcome.InvalidRequest, 0, 0);
        if (!items.TryGetValue(itemId, out var item)) return new Result(Outcome.ItemUnavailable, 0, 0);

        // A forged request must never cancel another operator's legitimate activation.
        var session = item.Session;
        if (session != null && session.Actor != authenticatedActor)
            return Respond(Outcome.Busy, item);

        if (session != null && session.Target != targetActor)
        {
            item.Session = null;
            session = null;
        }

        var observation = world.Observe(authenticatedActor, itemId, targetActor);
        if (!Eligible(observation) || completedLives.Contains(LifeKey(targetActor, observation.TargetLifeId)))
        {
            item.Session = null;
            return Respond(Outcome.InvalidTarget, item);
        }
        if (item.Charge < settings.BatteryCost)
        {
            item.Session = null;
            return Respond(Outcome.EmptyBattery, item);
        }

        if (session != null && (session.Damage != observation.DamageRevision ||
            session.Life != observation.TargetLifeId))
        {
            item.Session = null;
            return Respond(Outcome.Interrupted, item);
        }

        if (session == null)
        {
            foreach (var other in items.Values)
            {
                if (other.Session != null && (other.Session.Target == targetActor ||
                    other.Session.Actor == authenticatedActor)) return Respond(Outcome.Busy, item);
            }
            item.Session = new Session
            {
                Actor = authenticatedActor, Target = targetActor,
                Life = observation.TargetLifeId, Damage = observation.DamageRevision,
                Started = now, LastHeartbeat = now
            };
            return Respond(Outcome.Charging, item);
        }

        session.LastHeartbeat = now;
        double progress = Math.Min(1, (now - session.Started) / settings.HoldSeconds);
        if (progress < 1) return new Result(Outcome.Charging, progress, item.Charge);

        // Clear before calling the game adapter. Failed or throwing callbacks cannot leave a lease behind.
        item.Session = null;
        bool revived;
        inCommit = true;
        try
        {
            revived = world.TryRevive(authenticatedActor, targetActor, session.Life, settings.RevivalHealth);
        }
        finally
        {
            inCommit = false;
        }
        if (!revived) return Respond(Outcome.RevivalFailed, item);
        completedLives.Add(LifeKey(targetActor, session.Life));
        item.Charge -= settings.BatteryCost;
        return new Result(Outcome.Revived, 1, item.Charge);
    }

    public Result Release(int authenticatedActor, string itemId)
    {
        EnsureNotCommitting();
        if (!items.TryGetValue(itemId, out var item)) return new Result(Outcome.ItemUnavailable, 0, 0);
        if (item.Session != null && item.Session.Actor != authenticatedActor) return Respond(Outcome.Busy, item);
        item.Session = null;
        return Respond(Outcome.Cancelled, item);
    }

    /// <summary>
    /// Run every host frame, including when no network packets arrive. This detects damage,
    /// item drops, line-of-sight loss and movement between hold messages.
    /// </summary>
    public void Tick()
    {
        EnsureNotCommitting();
        Expire(ReadTime());
        foreach (var pair in items)
        {
            var session = pair.Value.Session;
            if (session == null) continue;
            var observation = world.Observe(session.Actor, pair.Key, session.Target);
            if (!Eligible(observation) || observation.DamageRevision != session.Damage ||
                observation.TargetLifeId != session.Life) pair.Value.Session = null;
        }
    }

    /// <summary>Disconnects and host migration cancel holds. Preserve battery values separately.</summary>
    public void CancelActor(int actorId)
    {
        EnsureNotCommitting();
        foreach (var item in items.Values)
            if (item.Session != null && (item.Session.Actor == actorId || item.Session.Target == actorId))
                item.Session = null;
    }

    public void CancelAll()
    {
        EnsureNotCommitting();
        foreach (var item in items.Values) item.Session = null;
    }

    private void Expire(double now)
    {
        foreach (var item in items.Values)
            if (item.Session != null && now - item.Session.LastHeartbeat > settings.HeartbeatTimeoutSeconds)
                item.Session = null;
    }

    private bool Eligible(Observation observation) => observation != null &&
        observation.RoundAllowsRevival && observation.OperatorAlive && observation.OperatorHoldsItem &&
        observation.TargetDead && observation.HasLineOfSight &&
        Settings.Finite(observation.DistanceMeters) && observation.DistanceMeters >= 0 &&
        observation.DistanceMeters <= settings.RangeMeters && !string.IsNullOrWhiteSpace(observation.TargetLifeId);

    private double ReadTime()
    {
        double now = clock();
        if (!Settings.Finite(now) || now < lastTime) throw new InvalidOperationException("Clock must be finite and monotonic.");
        lastTime = now;
        return now;
    }

    private void EnsureNotCommitting()
    {
        if (inCommit) throw new InvalidOperationException("Revival callbacks must not reenter the service.");
    }

    private static void ValidateCharge(int charge)
    {
        if (charge < 0 || charge > 100) throw new ArgumentOutOfRangeException(nameof(charge));
    }

    private static void ValidateItemId(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) throw new ArgumentException("An item ID is required.", nameof(itemId));
    }

    private static string LifeKey(int target, string life) => target + ":" + life;
    private static Result Respond(Outcome outcome, ItemState item) => new Result(outcome, 0, item.Charge);
}
