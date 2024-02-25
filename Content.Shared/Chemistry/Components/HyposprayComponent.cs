using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;

namespace Content.Shared.Chemistry.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HyposprayComponent : Component
{
    [DataField("solutionName")]
    public string SolutionName = "hypospray";

    // TODO: This should be on clumsycomponent.
    [DataField("clumsyFailChance")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float ClumsyFailChance = 0.5f;

    [DataField("transferAmount")]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 TransferAmount = FixedPoint2.New(5);

    [DataField("injectSound")]
    public SoundSpecifier InjectSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");

    /// <summary>
    /// Whether or not the hypo is able to inject only into mobs. On false you can inject into beakers/jugs
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public HyposprayToggleMode ToggleMode = HyposprayToggleMode.All;
}

/// <summary>
/// Possible modes for an <see cref="HyposprayComponent"/>.
/// </summary>
[Serializable, NetSerializable]
public enum HyposprayToggleMode : byte
{
    /// <summary>
    /// The hypospray will inject all targets
    /// </summary>
    All,

    /// <summary>
    /// The hypospray will inject mobs and draw from containers
    /// </summary>
    OnlyMobs
}
