using Robust.Shared.GameObjects;

namespace Content.Shared.BloodCult;

/// <summary>
/// Raised after a blood cultist is deconverted (e.g. by mindshield implant).
/// Server subscribes to log the deconversion for admin/antag tracking.
/// </summary>
public sealed class BloodCultDeconvertedEvent : EntityEventArgs
{
    public readonly EntityUid Entity;

    public BloodCultDeconvertedEvent(EntityUid entity)
    {
        Entity = entity;
    }
}
