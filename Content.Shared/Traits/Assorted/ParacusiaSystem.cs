using Content.Shared.GameTicking;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Random;
using System;


namespace Content.Shared.Traits.Assorted;
public sealed class ParacusiaSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ParacusiaComponent, ComponentGetState>(GetCompState);
        SubscribeLocalEvent<ParacusiaComponent, ComponentHandleState>(HandleCompState);
        SubscribeLocalEvent<ParacusiaComponent, ComponentStartup>(SetupParacusia);
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
        Dirty(component);
    }

    private void HandleCompState(EntityUid uid, ParacusiaComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ParacusiaComponentState state) return;
        component.MaxTimeBetweenIncidents = state.MaxTimeBetweenIncidents;
        component.MinTimeBetweenIncidents = state.MinTimeBetweenIncidents;
        component.MaxSoundDistance = state.MaxSoundDistance;
        component.Sounds = state.Sounds;
    }

    private void SetupParacusia(EntityUid uid, ParacusiaComponent component, ComponentStartup args)
    {
        component.NextIncidentTime =
            _random.NextFloat(component.MinTimeBetweenIncidents, component.MaxTimeBetweenIncidents);
    }

    private void ShutdownParacusia(RoundRestartCleanupEvent ev)
    {
        RemComp<ParacusiaComponent>(paracusia.Owner);
    }
}
