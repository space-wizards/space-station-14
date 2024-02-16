using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Tools.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WeldableComponent : Component
{
    /// <summary>
    ///     Tool quality for welding.
    /// </summary>
    [DataField("weldingQuality", customTypeSerializer: typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
    [ViewVariables(VVAccess.ReadWrite)]
    public string WeldingQuality = "Welding";

    /// <summary>
    ///     How much time does it take to weld/unweld entity.
    /// </summary>
    [DataField("time")]
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public TimeSpan WeldingTime = TimeSpan.FromSeconds(1f);

    /// <summary>
    ///     Shown when welded entity is examined.
    /// </summary>
    [DataField("weldedExamineMessage")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? WeldedExamineMessage = "weldable-component-examine-is-welded";

    /// <summary>
    ///     Is this entity currently welded shut?
    /// </summary>
    [DataField("isWelded"), AutoNetworkedField]
    public bool IsWelded;
}

[Serializable, NetSerializable]
public enum WeldableVisuals : byte
{
    IsWelded
}

[Serializable, NetSerializable]
public enum WeldableLayers : byte
{
    BaseWelded
}
