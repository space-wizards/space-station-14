using Content.Client.Items;
using Content.Shared.Weapons.Ranged;
using Robust.Client.UserInterface;

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

        if (ev.Control == null)
        {
            // TODO: Default control
        }

        component.Control = ev.Control;
    }

    public void UpdateAmmoCount(AmmoCounterComponent component)
    {
        if (component.Control == null) return;

        // TODO: Raise an event getting ammo count.
        // TODO: Update control.
    }

    /// <summary>
    /// Raised when an ammocounter is requesting a control
    /// </summary>
    public sealed class AmmoCounterControlEvent : HandledEntityEventArgs
    {
        public Control? Control;
    }
}
