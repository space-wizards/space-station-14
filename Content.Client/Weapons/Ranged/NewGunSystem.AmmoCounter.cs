using Content.Client.Items;
using Content.Client.Stylesheets;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Client.Graphics;
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
        UpdateAmmoCount(uid, component);
    }

    private void UpdateAmmoCount(EntityUid uid, AmmoCounterComponent component)
    {
        if (component.Control == null) return;

        var ev = new UpdateAmmoCounterEvent()
        {
            Control = component.Control
        };
        RaiseLocalEvent(uid, ev);
    }

    public override void UpdateAmmoCount(EntityUid uid)
    {
        // Don't use resolves because the method is shared and there's no compref and I'm trying to
        // share as much code as possible
        if (!TryComp<AmmoCounterComponent>(uid, out var clientComp)) return;

        UpdateAmmoCount(uid, clientComp);
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

    #region Controls

    public sealed class BoxesStatusControl : Control
    {
        private readonly BoxContainer _bulletsList;
        private readonly Label _ammoCount;

        public BoxesStatusControl()
        {
            MinHeight = 15;
            HorizontalExpand = true;
            VerticalAlignment = VAlignment.Center;

            AddChild(new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                Children =
                {
                    new Control
                    {
                        HorizontalExpand = true,
                        Children =
                        {
                            (_bulletsList = new BoxContainer
                            {
                                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                                VerticalAlignment = VAlignment.Center,
                                SeparationOverride = 4
                            }),
                        }
                    },
                    new Control() { MinSize = (5, 0) },
                    (_ammoCount = new Label
                    {
                        StyleClasses = { StyleNano.StyleClassItemStatus },
                        HorizontalAlignment = HAlignment.Right,
                    }),
                }
            });
        }

        public void Update(int count, int max)
        {
            _bulletsList.RemoveAllChildren();

            _ammoCount.Visible = true;

            _ammoCount.Text = $"x{count:00}";
            max = Math.Min(max, 8);
            FillBulletRow(_bulletsList, count, max);
        }

        private static void FillBulletRow(Control container, int count, int capacity)
        {
            var colorGone = Color.FromHex("#000000");
            var color = Color.FromHex("#E00000");

            // Draw the empty ones
            for (var i = count; i < capacity; i++)
            {
                container.AddChild(new PanelContainer
                {
                    PanelOverride = new StyleBoxFlat()
                    {
                        BackgroundColor = colorGone,
                    },
                    MinSize = (10, 15),
                });
            }

            // Draw the full ones, but limit the count to the capacity
            count = Math.Min(count, capacity);
            for (var i = 0; i < count; i++)
            {
                container.AddChild(new PanelContainer
                {
                    PanelOverride = new StyleBoxFlat()
                    {
                        BackgroundColor = color,
                    },
                    MinSize = (10, 15),
                });
            }
        }
    }

    #endregion
}
