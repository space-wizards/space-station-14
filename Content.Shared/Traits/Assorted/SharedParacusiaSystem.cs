using Content.Shared.GameTicking;
using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

public abstract class SharedParacusiaSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ParacusiaComponent, ComponentGetState>(GetCompState);
        SubscribeLocalEvent<ParacusiaComponent, ComponentHandleState>(HandleCompState);
    }

    private void GetCompState(EntityUid uid, ParacusiaComponent component, ref ComponentGetState args)
    {
        args.State = new ParacusiaComponentState(component.MaxTimeBetweenIncidents, component.MinTimeBetweenIncidents, component.MaxSoundDistance, component.Sounds);
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
}
