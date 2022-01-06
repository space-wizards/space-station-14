using Content.Shared.Smoking;
using Content.Server.Light.EntitySystems;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Server.GameObjects;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using Content.Shared.Light;

namespace Content.Server.Light.Components;

[RegisterComponent]
[Friend(typeof(CandleSystem))]

public class CandleComponent : Component
{
    public override string Name => "Candle";

    /// <summary>
    /// Current state to matchstick. Can be <code>Unlit</code>, <code>Lit</code> or <code>Burnt</code>.
    /// </summary>
    [ViewVariables]
    public SmokableState CurrentState = SmokableState.Unlit;


    /// <summary>
    /// State enum for determing what icon to use for the candle. There are 4 total states, BrandNew, Half, AlmostOut, Dead.
    /// </summary>
    [ViewVariables]
    public CandleState CurrentCandleIcon = CandleState.BrandNew;

    /// <summary>
    /// LightBehaviour behaviour ID. Used to trigger a specific light animation based on candle state
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("brandNewBehaviorID")]
    public string BrandNewBehaviourID { get; set; } = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("halfNewBehaviourID")]
    public string HalfNewBehaviorID { get; set; } = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("almostOutBehaviourID")]
    public string AlmostOutBehaviourID { get; set; } = string.Empty;

    /// <summary>
    /// Point light component so the candle can actually produce light
    /// </summary>
    [ComponentDependency]
    public readonly PointLightComponent? PointLightComponent = default!;
}
