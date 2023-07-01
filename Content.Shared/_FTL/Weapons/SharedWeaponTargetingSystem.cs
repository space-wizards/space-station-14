using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._FTL.Weapons;

/// <summary>
/// The system used for targeting and weapons.
/// </summary>
public abstract class SharedWeaponTargetingSystem : EntitySystem
{

}

[Serializable, NetSerializable]
public enum WeaponTargetingUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class FireWeaponSendMessage : BoundUserInterfaceMessage
{
    public EntityCoordinates Coordinates;
    public EntityUid TargetGrid;

    public FireWeaponSendMessage(EntityCoordinates coordinates, EntityUid targetGrid)
    {
        Coordinates = coordinates;
        TargetGrid = targetGrid;
    }
}

[Serializable, NetSerializable]
public sealed class WeaponTargetingUserInterfaceState : BoundUserInterfaceState
{
    public bool CanFire;
    public List<EntityUid> MapUids;

    public WeaponTargetingUserInterfaceState(bool canFire, List<EntityUid> mapUids)
    {
        CanFire = canFire;
        MapUids = mapUids;
    }
}
