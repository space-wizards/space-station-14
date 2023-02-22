using Content.Shared.GameTicking;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Random;
using System;


namespace Content.Shared.Traits.Assorted;

public abstract class SharedParacusiaSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ParacusiaComponent, ComponentGetState>(GetCompState);
        SubscribeLocalEvent<ParacusiaComponent, ComponentHandleState>(HandleCompState);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(ShutdownParacusia);
    }

    private void GetCompState(EntityUid uid, ParacusiaComponent component, ref ComponentGetState args)
    {
        args.State = new ParacusiaComponentState
        {
            MaxTimeBetweenIncidents = component.MaxTimeBetweenIncidents,
            MinTimeBetweenIncidents = component.MinTimeBetweenIncidents,
            MaxSoundDistance = component.MaxSoundDistance,
            Sounds = component.Sounds,
        };
    }

    private void HandleCompState(EntityUid uid, ParacusiaComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ParacusiaComponentState state)
            return;
        component.MaxTimeBetweenIncidents = state.MaxTimeBetweenIncidents;
        component.MinTimeBetweenIncidents = state.MinTimeBetweenIncidents;
        component.MaxSoundDistance = state.MaxSoundDistance;
        component.Sounds = state.Sounds;
    }

    private void ShutdownParacusia(RoundRestartCleanupEvent ev)
    {
        foreach (var comp in EntityQuery<ParacusiaComponent>(true))
        {
            comp.Stream?.Stop();
            comp.Stream = null;
            var ent = comp.Owner;
            RemComp(ent, comp);
        }
    }
}
