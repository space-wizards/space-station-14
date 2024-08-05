using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedGunSystem))]
public sealed partial class BallisticAmmoProviderComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SoundSpecifier? SoundRack = new SoundPathSpecifier("/Audio/Weapons/Guns/Cock/smg_cock.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SoundSpecifier? SoundInsert = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/bullet_insert.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public EntProtoId? Proto;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public int Capacity = 30;

    public int Count => UnspawnedCount + Container.ContainedEntities.Count;

    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public int UnspawnedCount;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public EntityWhitelist? Whitelist;

    public Container Container = default!;

    // TODO: Make this use stacks when the typeserializer is done.
    // Realistically just point to the container.
    [DataField, AutoNetworkedField]
    public List<EntityUid> Entities = new();

    /// <summary>
    /// Is the magazine allowed to be manually cycled to eject a cartridge.
    /// </summary>
    /// <remarks>
    /// Set to false for entities like turrets to avoid users being able to cycle them.
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public bool Cycleable = true;

    /// <summary>
    /// Is it okay for this entity to directly transfer its valid ammunition into another provider?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool MayTransfer;

    /// <summary>
    /// DoAfter delay for filling a bullet into another ballistic ammo provider.
    /// </summary>
    [DataField]
    public TimeSpan FillDelay = TimeSpan.FromSeconds(0.5);
}
