using System;
using System.Collections.Generic;
using Defibrillator.Core;

var tests = new (string Name, Action Run)[]
{
    ("Tracker donor without an embedded display remains compatible", () =>
    {
        Equal(true, DonorRequirements.IsCompatible(DonorRequirements.Required));
    }),
    ("Embedded display is optional and does not change donor compatibility", () =>
    {
        Equal(true, DonorRequirements.IsCompatible(DonorRequirements.Required | DonorParts.BatteryDisplay));
    }),
    ("Native reviver is never accepted as the custom donor", () =>
    {
        Equal(false, DonorRequirements.IsCompatible(DonorRequirements.Required | DonorParts.NativeReviver));
    }),
    ("Incomplete donor reports missing physics and battery components", () =>
    {
        var available = DonorRequirements.Required & ~(DonorParts.Battery | DonorParts.Physics);
        Equal(false, DonorRequirements.IsCompatible(available));
        Equal(DonorParts.Battery | DonorParts.Physics, DonorRequirements.Missing(available));
    }),
    ("Empty donor fails closed and complete donor has no missing requirements", () =>
    {
        Equal(false, DonorRequirements.IsCompatible(DonorParts.None));
        Equal(DonorRequirements.Required, DonorRequirements.Missing(DonorParts.None));
        Equal(DonorParts.None, DonorRequirements.Missing(DonorRequirements.Required));
    }),
    ("High/low voice selection has two equally mapped coin outcomes", () =>
    {
        Equal(1.8f, VoiceEffect.ChoosePitch(0)); Equal(0.55f, VoiceEffect.ChoosePitch(1));
        Throws<ArgumentOutOfRangeException>(() => VoiceEffect.ChoosePitch(2));
    }),
    ("Voice expires at 180 seconds while aftershocks remain active", () =>
    {
        var effect = new VoiceEffect(1.8f, 180, 30, 30, "round", "effect");
        Equal(true, effect.HasVoice(179.99, "round", true));
        Equal(false, effect.HasVoice(180, "round", true));
        Equal(true, effect.IsActive(210, "round", true));
        Equal(true, effect.TryShock(210, "round", true));
    }),
    ("Death ends both voice and periodic shocks", () =>
    {
        var effect = new VoiceEffect(0.55f, 180, 30, 30, "round", "effect");
        Equal(false, effect.HasVoice(60, "round", false));
        Equal(false, effect.TryShock(60, "round", false)); Equal(0, effect.Sequence);
    }),
    ("Level changes end both voice and periodic shocks", () =>
    {
        var effect = new VoiceEffect(1.8f, 180, 30, 30, "round", "effect");
        Equal(false, effect.HasVoice(60, "next-round", true));
        Equal(false, effect.TryShock(60, "next-round", true));
    }),
    ("First shock waits 30 seconds and subsequent shocks wait another interval", () =>
    {
        var effect = new VoiceEffect(1.8f, 180, 30, 30, "round", "effect");
        Equal(false, effect.TryShock(29.99, "round", true));
        Equal(true, effect.TryShock(30, "round", true)); Equal(1, effect.Sequence);
        Equal(false, effect.TryShock(30, "round", true));
        Equal(false, effect.TryShock(59.99, "round", true));
        Equal(true, effect.TryShock(60, "round", true)); Equal(2, effect.Sequence);
    }),
    ("A long host hitch causes one shock instead of a catch-up burst", () =>
    {
        var effect = new VoiceEffect(1.8f, 180, 30, 30, "round", "effect");
        Equal(true, effect.TryShock(125, "round", true));
        Equal(false, effect.TryShock(125, "round", true)); Equal(155d, effect.NextShockAt);
    }),
    ("Effect snapshots preserve voice expiry and shock sequence", () =>
    {
        var original = new VoiceEffect(0.55f, 180, 30, 30, "round", "effect");
        original.TryShock(30, "round", true);
        var restored = new VoiceEffect(original.Pitch, original.VoiceExpiresAt, original.NextShockAt,
            original.ShockInterval, original.Round, original.Token, original.Sequence);
        Equal(1, restored.Sequence); Equal(false, restored.TryShock(59, "round", true));
        Equal(true, restored.TryShock(60, "round", true)); Equal(2, restored.Sequence);
    }),
    ("Non-finite effect times fail closed", () =>
    {
        var effect = new VoiceEffect(1.8f, 180, 30, 30, "round", "effect");
        Equal(false, effect.HasVoice(double.NaN, "round", true));
        Equal(false, effect.TryShock(double.PositiveInfinity, "round", true));
    }),
    ("Invalid effect parameters are rejected", () =>
    {
        Throws<ArgumentOutOfRangeException>(() => new VoiceEffect(3, 180, 30, 30, "round", "effect"));
        Throws<ArgumentOutOfRangeException>(() => new VoiceEffect(1.8f, 180, 30, 0, "round", "effect"));
        Throws<ArgumentException>(() => new VoiceEffect(1.8f, 180, 30, 30, "round", ""));
    }),
    ("Three seconds revives once at one HP and consumes 25 percent", () =>
    {
        var f = new Fixture();
        Equal(Outcome.Charging, f.Hold().Outcome);
        Equal(Outcome.Revived, f.Complete().Outcome);
        Equal(75, f.Service.GetCharge("a"));
        Equal(1, f.World.LastHealth);
        Equal(1, f.World.Revivals);
        Equal(Outcome.InvalidTarget, f.Hold().Outcome);
        Equal(1, f.World.Revivals);
    }),
    ("Progress uses host time and cannot complete early", () =>
    {
        var f = new Fixture(); f.Hold();
        for (var i = 1; i <= 11; i++) { f.Now = i * 0.25; Equal(Outcome.Charging, f.Hold().Outcome); }
        Equal(0, f.World.Revivals); Equal(100, f.Service.GetCharge("a"));
    }),
    ("Release preserves charge and resets progress", () =>
    {
        var f = new Fixture(); f.Hold(); f.Now = 0.5; f.Hold();
        Equal(Outcome.Cancelled, f.Service.Release(1, "a").Outcome);
        Equal(100, f.Service.GetCharge("a")); Equal(0d, f.Hold().Progress);
    }),
    ("Four uses exhaust battery and a fifth cannot start", () =>
    {
        var f = new Fixture();
        for (int i = 0; i < 4; i++)
        {
            f.World.Life = "death-" + i; f.World.Dead = true;
            f.Hold(); Equal(Outcome.Revived, f.Complete().Outcome);
        }
        f.World.Life = "fifth"; f.World.Dead = true;
        Equal(Outcome.EmptyBattery, f.Hold().Outcome); Equal(4, f.World.Revivals);
    }),
    ("Charger permits reuse without creating an extra item", () =>
    {
        var f = new Fixture(0); Equal(Outcome.EmptyBattery, f.Hold().Outcome);
        f.Service.UpdateChargeFromCharger("a", 25); f.Hold();
        Equal(Outcome.Revived, f.Complete().Outcome); Equal(0, f.Service.GetCharge("a"));
    }),
    ("Duplicate item registration cannot refill battery", () =>
    {
        var f = new Fixture(25); Throws<InvalidOperationException>(() => f.Service.RegisterItem("a"));
        Equal(25, f.Service.GetCharge("a"));
    }),
    ("Damage interrupts even if health has been restored", () =>
    {
        var f = new Fixture(); f.Hold(); f.World.Damage++;
        Equal(Outcome.Interrupted, f.Hold().Outcome); Equal(100, f.Service.GetCharge("a"));
    }),
    ("Frame tick catches damage between input packets", () =>
    {
        var f = new Fixture(); f.Hold(); f.Now = 0.25; f.World.Damage++;
        f.Service.Tick(); Equal(0d, f.Hold().Progress); Equal(0, f.World.Revivals);
    }),
    ("Frame tick catches a temporary excursion out of range", () =>
    {
        var f = new Fixture(); f.Hold(); f.Now = 0.25; f.World.Distance = 2.01;
        f.Service.Tick(); f.World.Distance = 1; Equal(0d, f.Hold().Progress);
    }),
    ("Range boundary is inclusive", () =>
    {
        var f = new Fixture(); f.World.Distance = 2; Equal(Outcome.Charging, f.Hold().Outcome);
        f.World.Distance = 2.001; Equal(Outcome.InvalidTarget, f.Hold().Outcome);
    }),
    ("Line of sight is required", () =>
    {
        var f = new Fixture(); f.World.Visible = false; Equal(Outcome.InvalidTarget, f.Hold().Outcome);
    }),
    ("Invalid distances fail closed", () =>
    {
        foreach (var distance in new[] { double.NaN, double.PositiveInfinity, -1d })
        { var f = new Fixture(); f.World.Distance = distance; Equal(Outcome.InvalidTarget, f.Hold().Outcome); }
    }),
    ("Living targets cannot be revived", () =>
    {
        var f = new Fixture(); f.World.Dead = false; Equal(Outcome.InvalidTarget, f.Hold().Outcome);
    }),
    ("Round transition disables revival", () =>
    {
        var f = new Fixture(); f.Hold(); f.World.InRound = false;
        Equal(Outcome.InvalidTarget, f.Hold().Outcome); Equal(100, f.Service.GetCharge("a"));
    }),
    ("Dropped item and dead operator interrupt", () =>
    {
        var f = new Fixture(); f.Hold(); f.World.Held = false;
        Equal(Outcome.InvalidTarget, f.Hold().Outcome);
        f.World.Held = true; f.World.Alive = false; Equal(Outcome.InvalidTarget, f.Hold().Outcome);
    }),
    ("A second operator cannot cancel or steal an item lease", () =>
    {
        var f = new Fixture(); f.Hold();
        Equal(Outcome.Busy, f.Service.Release(3, "a").Outcome);
        Equal(Outcome.Busy, f.Service.Hold(3, "a", 2).Outcome);
        Equal(Outcome.Revived, f.Complete().Outcome);
    }),
    ("Two defibrillators cannot reserve the same target", () =>
    {
        var f = new Fixture(); f.Service.RegisterItem("b"); f.Hold();
        Equal(Outcome.Busy, f.Service.Hold(3, "b", 2).Outcome);
        Equal(Outcome.Revived, f.Complete().Outcome); Equal(100, f.Service.GetCharge("b"));
    }),
    ("One actor cannot operate two defibrillators", () =>
    {
        var f = new Fixture(); f.Service.RegisterItem("b"); f.Hold();
        Equal(Outcome.Busy, f.Service.Hold(1, "b", 3).Outcome);
    }),
    ("Switching targets restarts the timer", () =>
    {
        var f = new Fixture(); f.Hold(); f.Now = 0.5; f.Hold();
        Equal(0d, f.Service.Hold(1, "a", 3).Progress);
    }),
    ("Missing heartbeat restarts instead of completing", () =>
    {
        var f = new Fixture(); f.Hold(); f.Now = 4;
        Equal(0d, f.Hold().Progress); Equal(0, f.World.Revivals);
    }),
    ("Tick expires abandoned leases and permits another operator", () =>
    {
        var f = new Fixture(); f.Hold(); f.Now = 1; f.Service.Tick();
        Equal(Outcome.Charging, f.Service.Hold(3, "a", 2).Outcome);
    }),
    ("Death instance changes interrupt old attempts", () =>
    {
        var f = new Fixture(); f.Hold(); f.World.Life = "another-death";
        Equal(Outcome.Interrupted, f.Hold().Outcome); Equal(0, f.World.Revivals);
    }),
    ("Failed game revival never spends battery", () =>
    {
        var f = new Fixture(); f.World.AcceptRevival = false; f.Hold();
        Equal(Outcome.RevivalFailed, f.Complete().Outcome); Equal(100, f.Service.GetCharge("a"));
        f.World.AcceptRevival = true; f.Hold(); Equal(Outcome.Revived, f.Complete().Outcome);
    }),
    ("Throwing adapter releases the lease without spending battery", () =>
    {
        var f = new Fixture(); f.World.Throw = true; f.Hold();
        Throws<InvalidOperationException>(() => f.Complete()); Equal(100, f.Service.GetCharge("a"));
        f.World.Throw = false; Equal(0d, f.Hold().Progress);
    }),
    ("Duplicate stale death snapshots cannot replay a completed revival", () =>
    {
        var f = new Fixture(); f.Hold(); f.Complete(); f.World.Dead = true;
        Equal(Outcome.InvalidTarget, f.Hold().Outcome); Equal(1, f.World.Revivals);
    }),
    ("Disconnected operator and host migration cancel progress", () =>
    {
        var f = new Fixture(); f.Hold(); f.Now = 0.5; f.Hold();
        f.Service.CancelActor(1); Equal(0d, f.Hold().Progress);
        f.Now = 1; f.Hold(); f.Service.CancelAll(); Equal(0d, f.Hold().Progress);
        Equal(100, f.Service.GetCharge("a"));
    }),
    ("Removing an item releases its target", () =>
    {
        var f = new Fixture(); f.Hold(); f.Service.RegisterItem("b"); f.Service.RemoveItem("a");
        Equal(Outcome.ItemUnavailable, f.Hold().Outcome);
        Equal(Outcome.Charging, f.Service.Hold(3, "b", 2).Outcome);
    }),
    ("Unauthenticated, self and unknown item requests fail", () =>
    {
        var f = new Fixture(); Equal(Outcome.InvalidRequest, f.Service.Hold(0, "a", 2).Outcome);
        Equal(Outcome.InvalidRequest, f.Service.Hold(1, "a", 1).Outcome);
        Equal(Outcome.ItemUnavailable, f.Service.Hold(1, "unknown", 2).Outcome);
    }),
    ("Clock regression and non-finite clock fail closed", () =>
    {
        var f = new Fixture(); f.Hold(); f.Now = -1; Throws<InvalidOperationException>(() => f.Hold());
        f.Now = double.NaN; Throws<InvalidOperationException>(() => f.Service.Tick());
    }),
    ("Settings and charge reject unsafe values", () =>
    {
        Throws<ArgumentOutOfRangeException>(() => new Settings(holdSeconds: double.NaN));
        Throws<ArgumentOutOfRangeException>(() => new Settings(batteryCost: 0));
        Throws<ArgumentOutOfRangeException>(() => new Settings(revivalHealth: 0));
        Throws<ArgumentOutOfRangeException>(() => new Settings(price: -1));
        var f = new Fixture(); Throws<ArgumentOutOfRangeException>(() => f.Service.UpdateChargeFromCharger("a", 101));
    }),
    ("Configured cost, health and duration are honored", () =>
    {
        var f = new Fixture(100, new Settings(holdSeconds: 1, batteryCost: 50, revivalHealth: 25));
        f.Hold(); for (int i = 1; i <= 3; i++) { f.Now = i * 0.25; f.Hold(); }
        f.Now = 1; Equal(Outcome.Revived, f.Hold().Outcome);
        Equal(50, f.Service.GetCharge("a")); Equal(25, f.World.LastHealth);
    })
};

int failed = 0;
foreach (var test in tests)
{
    try { test.Run(); Console.WriteLine("PASS " + test.Name); }
    catch (Exception exception) { failed++; Console.Error.WriteLine("FAIL " + test.Name + ": " + exception); }
}
Console.WriteLine($"Executed {tests.Length} tests: {tests.Length - failed} passed, {failed} failed.");
return failed == 0 ? 0 : 1;

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new Exception($"Expected {expected}; got {actual}.");
}

static void Throws<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T) { return; }
    throw new Exception("Expected " + typeof(T).Name);
}

sealed class Fixture
{
    public double Now;
    public readonly FakeWorld World = new FakeWorld();
    public readonly RevivalService Service;
    public Fixture(int charge = 100, Settings? settings = null)
    {
        Service = new RevivalService(settings ?? new Settings(), World, () => Now);
        Service.RegisterItem("a", charge);
    }
    public Result Hold() => Service.Hold(1, "a", 2);
    public Result Complete()
    {
        Result result = default;
        for (int i = 0; i < 12; i++) { Now += 0.25; result = Hold(); }
        return result;
    }
}

sealed class FakeWorld : IRevivalWorld
{
    public bool InRound = true, Alive = true, Held = true, Dead = true, Visible = true;
    public bool AcceptRevival = true, Throw;
    public double Distance = 1;
    public long Damage;
    public string Life = "death-1";
    public int Revivals, LastHealth;

    public Observation Observe(int actorId, string itemId, int targetActorId) =>
        new Observation(InRound, Alive, Held, Dead, Visible, Distance, Damage, Life);

    public bool TryRevive(int actorId, int targetActorId, string targetLifeId, int health)
    {
        if (Throw) throw new InvalidOperationException("Simulated game adapter exception.");
        if (!AcceptRevival || !Dead || targetLifeId != Life) return false;
        Revivals++; LastHealth = health; Dead = false; return true;
    }
}
