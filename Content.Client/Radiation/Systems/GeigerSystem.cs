using Content.Shared.Radiation.Components;
using Robust.Shared.GameStates;

namespace Content.Client.Radiation.Systems;

public sealed class GeigerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeigerComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, GeigerComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not GeigerComponentState state)
            return;

        component.CurrentRadiation = state.CurrentRadiation;
    }
}
