using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Weapons.Ranged;

[RegisterComponent, NetworkedComponent]
public sealed class BallisticAmmoProviderComponent : Component
{
    // Will either spawn ammo as required or can potentially have stuff inserted
    [ViewVariables, DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    [ViewVariables, DataField("fillProto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? FillProto;

    [ViewVariables(VVAccess.ReadWrite), DataField("capacity")]
    public int Capacity = 30;

    [ViewVariables, DataField("unspawnedCount")]
    public int UnspawnedCount;

    public Container Container = default!;

    [ViewVariables, DataField("entities")]
    public Stack<EntityUid> Entities = new();

    /// <summary>
    /// Will the ammoprovider automatically cycle through rounds or does it need doing manually.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("autoCycle")]
    public bool AutoCycle = true;

    /// <summary>
    /// Is the gun ready to shoot; if AutoCycle is true then this will always stay true and not need to be manually done.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("cycled")]
    public bool Cycled = true;
}
