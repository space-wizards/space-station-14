using System.Numerics;
using Content.Shared._Starlight.Combat.Ranged.Pierce;
using Content.Shared.Inventory;
using Content.Shared.Weapons.Reflect;

namespace Content.Shared._Starlight.Weapon;

[ByRefEvent]
public record struct HitScanPierceAttemptEvent(PierceLevel Level, bool Pierced) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => ~SlotFlags.POCKET;
}
