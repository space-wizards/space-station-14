using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised on an AmmoProvider to request deets.
/// </summary>
[ByRefEvent]
public struct GetAmmoCountEvent
{
    public int Count;
    public int Capacity;

    // DS14-start: allow read-only inspection of the next shot without consuming ammunition.
    public EntityUid? NextAmmoEntity;
    public EntProtoId? NextAmmoPrototype;
    // DS14-end
}
