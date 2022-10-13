using Content.Server.Radiation.Components;
using Content.Server.Radiation.Events;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Robust.Shared.GameStates;

namespace Content.Server.Radiation.Systems;

public sealed class GeigerSystem : SharedGeigerSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeigerComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<OnRadiationSystemUpdated>(OnUpdate);
    }

    private void OnUpdate(OnRadiationSystemUpdated ev)
    {
        var query = EntityQuery<GeigerComponent, RadiationReceiverComponent>();
        foreach (var (geiger, receiver) in query)
        {
            var rads = receiver.CurrentRadiation;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (rads == geiger.CurrentRadiation)
                continue;

            geiger.CurrentRadiation = rads;
            Dirty(geiger);
        }
    }

    private void OnGetState(EntityUid uid, GeigerComponent component, ref ComponentGetState args)
    {
        args.State = new GeigerComponentState
        {
            CurrentRadiation = component.CurrentRadiation
        };
    }
}


