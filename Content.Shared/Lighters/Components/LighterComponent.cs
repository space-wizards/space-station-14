using Content.Shared.Lighter.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;


namespace Content.Shared.Lighter.Components;
/// <summary>
/// Handles fuel consumption for lighters.
/// Created to decouple lighters from the welder/shared tools systems.
/// </summary>
/// <summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(LighterSystem))]
public sealed partial class LighterComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField]
    public TimeSpan LighterUpdateTimer = TimeSpan.FromSeconds(1);

    [DataField]
    public string FuelSolutionName = "lighter";

    [DataField]
    public ProtoId<ReagentPrototype> FuelReagent = "WeldingFuel";

    [DataField, AutoNetworkedField]
    public FixedPoint2 FuelConsumption = FixedPoint2.New(1.0f);

    [DataField, AutoNetworkedField]
    public FixedPoint2 FuelLitCost = FixedPoint2.New(0.5f);

    [DataField]
    public SoundSpecifier LighterRefill = new SoundPathSpecifier("/Audio/Effects/refill.ogg");

}

