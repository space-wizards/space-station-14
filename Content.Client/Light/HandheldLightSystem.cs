using Content.Client.Items.Systems;
using Content.Client.Light.Components;
using Content.Shared.Item;
using Content.Shared.Light.Component;
using Robust.Shared.GameStates;

namespace Content.Client.Light;

public sealed class HandheldLightSystem : EntitySystem
{
    [Dependency] private readonly ItemSystem _itemSys = default!;

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

        if (state.Activated == component.Activated)
            return;

        component.Activated = state.Activated;

        // really hand-held lights should be using a separate unshaded layer. (see FlashlightVisualizer)
        // this prefix stuff is largely for backwards compatibility with RSIs/yamls that have not been updated.
        if (component.AddPrefix && TryComp(uid, out SharedItemComponent? item))
        {
            item.EquippedPrefix = state.Activated ? "on" : "off";
            _itemSys.VisualsChanged(uid);
        }
    }
}
