using Content.Client.IoC;
using Content.Client.Items;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

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

        // Fallback to default if none specified
        ev.Control ??= new DefaultStatusControl();

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
        if (!Timing.IsFirstTimePredicted ||
            !TryComp<AmmoCounterComponent>(uid, out var clientComp)) return;

        UpdateAmmoCount(uid, clientComp);
    }

    /// <summary>
    /// Raised when an ammocounter is requesting a control.
    /// </summary>
    public sealed class AmmoCounterControlEvent : EntityEventArgs
    {
        public Control? Control;
    }

    /// <summary>
    /// Raised whenever the ammo count / magazine for a control needs updating.
    /// </summary>
    public sealed class UpdateAmmoCounterEvent : HandledEntityEventArgs
    {
        public Control Control = default!;
    }

    #region Controls

    private sealed class DefaultStatusControl : Control
    {
        private readonly BoxContainer _bulletsListTop;
        private readonly BoxContainer _bulletsListBottom;

        public DefaultStatusControl()
        {
            MinHeight = 15;
            HorizontalExpand = true;
            VerticalAlignment = VAlignment.Center;
            AddChild(new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                HorizontalExpand = true,
                VerticalAlignment = VAlignment.Center,
                SeparationOverride = 0,
                Children =
                {
                    (_bulletsListTop = new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Horizontal,
                        SeparationOverride = 0
                    }),
                    new BoxContainer
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
                                    (_bulletsListBottom = new BoxContainer
                                    {
                                        Orientation = BoxContainer.LayoutOrientation.Horizontal,
                                        VerticalAlignment = VAlignment.Center,
                                        SeparationOverride = 0
                                    }),
                                }
                            },
                        }
                    }
                }
            });
        }

        public void Update(int count, int capacity)
        {
            _bulletsListTop.RemoveAllChildren();
            _bulletsListBottom.RemoveAllChildren();

            string texturePath;
            if (capacity <= 20)
            {
                texturePath = "/Textures/Interface/ItemStatus/Bullets/normal.png";
            }
            else if (capacity <= 30)
            {
                texturePath = "/Textures/Interface/ItemStatus/Bullets/small.png";
            }
            else
            {
                texturePath = "/Textures/Interface/ItemStatus/Bullets/tiny.png";
            }

            var texture = StaticIoC.ResC.GetTexture(texturePath);

            const int tinyMaxRow = 60;

            if (capacity > tinyMaxRow)
            {
                FillBulletRow(_bulletsListBottom, Math.Min(tinyMaxRow, count), tinyMaxRow, texture);
                FillBulletRow(_bulletsListTop, Math.Max(0, count - tinyMaxRow), capacity - tinyMaxRow, texture);
            }
            else
            {
                FillBulletRow(_bulletsListBottom, count, capacity, texture);
            }
        }

        private static void FillBulletRow(Control container, int count, int capacity, Texture texture)
        {
            var colorA = Color.FromHex("#b68f0e");
            var colorB = Color.FromHex("#d7df60");
            var colorGoneA = Color.FromHex("#000000");
            var colorGoneB = Color.FromHex("#222222");

            var altColor = false;

            for (var i = count; i < capacity; i++)
            {
                container.AddChild(new TextureRect
                {
                    Texture = texture,
                    ModulateSelfOverride = altColor ? colorGoneA : colorGoneB
                });

                altColor ^= true;
            }

            for (var i = 0; i < count; i++)
            {
                container.AddChild(new TextureRect
                {
                    Texture = texture,
                    ModulateSelfOverride = altColor ? colorA : colorB
                });

                altColor ^= true;
            }
        }
    }

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
