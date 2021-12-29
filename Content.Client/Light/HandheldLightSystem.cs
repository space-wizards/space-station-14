using Content.Client.Light.Components;
using Content.Shared.Light.Component;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.Light;

public sealed class HandheldLightSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HandheldLightComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, HandheldLightComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not SharedHandheldLightComponent.HandheldLightComponentState state)
            return;

        component.Level = state.Charge;
    }
}
