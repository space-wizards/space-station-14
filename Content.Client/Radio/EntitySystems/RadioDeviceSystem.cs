using Content.Client.Radio.Ui;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Radio.EntitySystems;
using Robust.Client.GameObjects;

namespace Content.Client.Radio.EntitySystems;

/// <inheritdoc/>
public sealed partial class RadioDeviceSystem : SharedRadioDeviceSystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;

    [SubscribeLocalEvent]
    private void OnAfterHandleState(Entity<IntercomComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_ui.TryGetOpenUi<IntercomBoundUserInterface>(ent.Owner, IntercomUiKey.Key, out var bui))
            bui.Update(ent);
    }
}
