using Content.Client.IoC;
using Content.Client.Items;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.Weapons.Ranged.Barrels.Components;
using Content.Shared.Weapons.Ranged;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Weapons.Ranged;

public sealed partial class NewGunSystem
{
    private void OnAmmoCounterCollect(EntityUid uid, AmmoCounterComponent component, ItemStatusCollectMessage args)
    {
        RefreshControl(uid, component);

        if (component.Control != null)
            args.Controls.Add(component.Control);
    }

    /// <summary>
    /// Refreshes the control being used to show ammo. Useful if you change the AmmoProvider.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    public void RefreshControl(EntityUid uid, AmmoCounterComponent? component = null)
    {
        if (!Resolve(uid, ref component, false)) return;

        component.Control?.Dispose();
        component.Control = null;

        var ev = new AmmoCounterControlEvent();
        RaiseLocalEvent(uid, ev);

        if (!ev.Handled)
        {
            // TODO: Default control
        }

        component.Control = ev.Control;
    }

    public override void UpdateAmmoCount(EntityUid uid)
    {
        // Don't use resolves because the method is shared and there's no compref and I'm trying to
        // share as much code as possible
        if (!TryComp<AmmoCounterComponent>(uid, out var clientComp)) return;

        if (clientComp.Control == null) return;

        var ev = new UpdateAmmoCounterEvent()
        {
            Control = clientComp.Control
        };
        RaiseLocalEvent(uid, ev);
    }

    /// <summary>
    /// Raised when an ammocounter is requesting a control
    /// </summary>
    public sealed class AmmoCounterControlEvent : HandledEntityEventArgs
    {
        public Control? Control;
    }

    public sealed class UpdateAmmoCounterEvent : HandledEntityEventArgs
    {
        public Control Control = default!;
    }
}
