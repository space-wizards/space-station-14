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

    /// <summary>
    /// Transfer DNA from one entity onto the forensics of another.
    /// </summary>
    /// <param name="recipient">The entity receiving the DNA.</param>
    /// <param name="donor">The entity applying its DNA.</param>
    /// <param name="canDnaBeCleaned">If this DNA be cleaned off of the recipient. e.g. cleaning a knife vs cleaning a puddle of blood.</param>
    public virtual void TransferDna(EntityUid recipient, EntityUid donor, bool canDnaBeCleaned = true) { }

}
