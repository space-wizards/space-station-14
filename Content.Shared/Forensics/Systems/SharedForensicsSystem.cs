using Content.Shared.Forensics.Components;

namespace Content.Shared.Forensics.Systems;

public abstract class SharedForensicsSystem : EntitySystem
{
    /// <summary>
    /// Give the entity a new, random DNA string and call an event to notify other systems like the bloodstream that it has been changed.
    /// Does nothing if it does not have the DnaComponent.
    /// </summary>
    public virtual void RandomizeDNA(Entity<DnaComponent?> ent) { }

    /// <summary>
    /// Give the entity a new, random fingerprint string.
    /// Does nothing if it does not have the FingerprintComponent.
    /// </summary>
    public virtual void RandomizeFingerprint(Entity<FingerprintComponent?> ent) { }
}
