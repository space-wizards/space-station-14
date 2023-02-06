using Content.Shared.GameTicking;
using Robust.Shared.GameStates;


namespace Content.Shared.Traits.Assorted;
public sealed class ParacusiaSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ParacusiaComponent, ComponentGetState>(GetCompState);
        SubscribeLocalEvent<ParacusiaComponent, ComponentHandleState>(HandleCompState);
        SubscribeLocalEvent<ParacusiaComponent, RoundRestartCleanupEvent>(OnShutdown);
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

    public void OnShutdown(EntityUid uid, ParacusiaComponent component, RoundRestartCleanupEvent ev)
    {
        RemComp(component.Owner, component);
    }
}
