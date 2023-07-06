using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.CallERT;

[RegisterComponent]
public sealed class StationCallErtComponent : Component
{
    [ViewVariables]
    public ErtGroupsPrototype? ErtGroups;

    [ViewVariables]
    public ErtGroupDetail? CalledErtGroup;

    // Once stations are a prototype, this should be used.
    [DataField("ertGroupsPrototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<ErtGroupsPrototype>))]
    public string ErtGroupsPrototype = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool ErtCalled = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public float CallErtCooldownRemaining = 0;
    [ViewVariables(VVAccess.ReadWrite)]
    public float NewCallErtCooldownRemaining = 0;
}
