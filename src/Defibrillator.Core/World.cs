namespace Defibrillator.Core;

/// <summary>
/// Trusted, freshly observed host state. Never deserialize this object from a client.
/// Life IDs must change on every death, even if the same player dies repeatedly.
/// </summary>
public sealed class Observation
{
    public bool RoundAllowsRevival { get; }
    public bool OperatorAlive { get; }
    public bool OperatorHoldsItem { get; }
    public bool TargetDead { get; }
    public bool HasLineOfSight { get; }
    public double DistanceMeters { get; }
    public long DamageRevision { get; }
    public string TargetLifeId { get; }

    public Observation(bool roundAllowsRevival, bool operatorAlive, bool operatorHoldsItem,
        bool targetDead, bool hasLineOfSight, double distanceMeters, long damageRevision,
        string targetLifeId)
    {
        RoundAllowsRevival = roundAllowsRevival;
        OperatorAlive = operatorAlive;
        OperatorHoldsItem = operatorHoldsItem;
        TargetDead = targetDead;
        HasLineOfSight = hasLineOfSight;
        DistanceMeters = distanceMeters;
        DamageRevision = damageRevision;
        TargetLifeId = targetLifeId;
    }
}

/// <summary>Implemented by the version-specific game adapter on the Unity main thread.</summary>
public interface IRevivalWorld
{
    Observation Observe(int actorId, string itemId, int targetActorId);

    /// <summary>
    /// Atomically validate the same death instance, revive once, and apply exact health.
    /// Return true only after success is confirmed. Must not call back into the service.
    /// A false result must leave the target dead. Client-supplied health is never accepted.
    /// </summary>
    bool TryRevive(int actorId, int targetActorId, string targetLifeId, int health);
}
