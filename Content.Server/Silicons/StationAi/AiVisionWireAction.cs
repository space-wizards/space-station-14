using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationAi;
using Content.Shared.Wires;

namespace Content.Server.Silicons.StationAi;

/// <summary>
/// Handles StationAiVision functionality for the attached entity.
/// </summary>
public sealed partial class AiVisionWireAction : ComponentWireAction<StationAiVisionComponent>
{
    public override string Name { get; set; } = "wire-name-ai-vision-light";
    public override Color Color { get; set; } = Color.White;
    public override object StatusKey => AirlockWireStatus.AiVisionIndicator;

    public override StatusLightState? GetLightState(Wire wire, StationAiVisionComponent component)
    {
        return component.Enabled ? StatusLightState.On : StatusLightState.Off;
    }

    public override bool Cut(EntityUid user, Wire wire, StationAiVisionComponent component)
    {
        return EntityManager.System<SharedStationAiSystem>()
            .SetVisionEnabled((component.Owner, component), false, announce: true);
    }

    public override bool Mend(EntityUid user, Wire wire, StationAiVisionComponent component)
    {
        return EntityManager.System<SharedStationAiSystem>()
            .SetVisionEnabled((component.Owner, component), true);
    }

    public override void Pulse(EntityUid user, Wire wire, StationAiVisionComponent component)
    {
        // TODO: This should turn it off for a bit
        // Need timer cleanup first out of scope.
    }
}
