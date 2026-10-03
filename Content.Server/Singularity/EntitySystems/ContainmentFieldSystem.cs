using Content.Server.Singularity.Events;
using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;

namespace Content.Server.Singularity.EntitySystems;

public sealed partial class ContainmentFieldSystem : SharedContainmentFieldSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ContainmentFieldComponent, EventHorizonAttemptConsumeEntityEvent>(HandleEventHorizon);
    }

    private void HandleEventHorizon(EntityUid uid, ContainmentFieldComponent component, ref EventHorizonAttemptConsumeEntityEvent args)
    {
        if(!args.Cancelled && !args.EventHorizon.CanBreachContainment)
            args.Cancelled = true;
    }
}
