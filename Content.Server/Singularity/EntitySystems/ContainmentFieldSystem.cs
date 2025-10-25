using Content.Server.Singularity.Events;
using Content.Shared.Popups;
using Content.Shared.Shuttles.Components;
using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;
using Robust.Shared.Physics.Events;

namespace Content.Server.Singularity.EntitySystems;

public sealed class ContainmentFieldSystem : SharedContainmentFieldSystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

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
