using Content.Shared.Hands.Components;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem
{
    private void InitializeVirtual()
    {
        SubscribeLocalEvent<HandVirtualItemComponent, AfterAutoHandleStateEvent>(OnVirtualAfter);
    }

    private void OnVirtualAfter(EntityUid uid, HandVirtualItemComponent component, ref AfterAutoHandleStateEvent args)
    {
        // update hands GUI with new entity.
        if (_containerSystem.IsEntityInContainer(uid))
            _items.VisualsChanged(uid);
    }
}
