using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Lube;

/// <summary>
/// Used by lube bottles to apply <see cref="LubedComponent"/> to an item.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LubeComponent : Component
{
    [DataField, AutoNetworkedField]
    public SoundSpecifier Squeeze = new SoundPathSpecifier("/Audio/Items/squeezebottle.ogg");

    /// <summary>
    /// Solution on the entity that contains the glue.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Solution = "drink";

    /// <summary>
    /// Reagent that will be used as glue.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype> Reagent = "SpaceLube";

    /// <summary>
    /// Reagent consumption per use.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Consumption = FixedPoint2.New(3);

    /// <summary>
    /// Min slips per unit
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MinSlips = 1;

    /// <summary>
    /// Max slips per unit
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxSlips = 6;

    /// <summary>
    /// The velocity the lubed item will be thrown at.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SlipStrength = 10.0f;
}
