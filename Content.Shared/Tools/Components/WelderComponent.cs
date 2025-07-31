using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Tools.Components;

/// <summary>
/// Handles fuel consumption for the tool and allows it to explode welding fuel tanks.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedToolSystem))]
public sealed partial class WelderComponent : Component
{
    /// <summary>
    /// Is the welder currently enabled?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled;

    /// <summary>
    /// Timestamp for the next update loop update.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate;

    /// <summary>
    /// Delay between updates.
    /// </summary>
    [DataField]
    public TimeSpan WelderUpdateTimer = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Name of the fuel solution.
    /// </summary>
    [DataField]
    public string FuelSolutionName = "Welder";

    /// <summary>
    /// Reagent that will be used as fuel for welding.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> FuelReagent = "WeldingFuel";

    /// <summary>
    /// Fuel consumption per second while the welder is active.
    /// In u/s
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 FuelConsumption = FixedPoint2.New(1.0f);

    /// <summary>
    /// A fuel amount to be consumed when the welder goes from being unlit to being lit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 FuelLitCost = FixedPoint2.New(0.5f);

    /// <summary>
    /// Sound played when refilling the welder.
    /// </summary>
    [DataField]
    public SoundSpecifier WelderRefill = new SoundPathSpecifier("/Audio/Effects/refill.ogg");

    /// <summary>
    /// Whether the item is safe to refill while lit without exploding the tank.
    /// </summary>
    [DataField]
    public bool TankSafe;
}
