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

    public override void UpdateAmmoCount(EntityUid uid, SharedAmmoCounterComponent? component = null)
    {
        if (!Resolve(uid, ref component, false) ||
            component is not AmmoCounterComponent clientComp) return;

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

    private sealed class DefaultAmmoCountControl : Control
    {
        private readonly BoxContainer _bulletsList;
        private readonly TextureRect _chamberedBullet;
        private readonly Label _noMagazineLabel;
        private readonly Label _ammoCount;

        public DefaultAmmoCountControl(ClientMagazineBarrelComponent parent)
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
                    (_chamberedBullet = new TextureRect
                    {
                        Texture = StaticIoC.ResC.GetTexture("/Textures/Interface/ItemStatus/Bullets/chambered_rotated.png"),
                        VerticalAlignment = VAlignment.Center,
                        HorizontalAlignment = HAlignment.Right,
                    }),
                    new Control() { MinSize = (5,0) },
                    new Control
                    {
                        HorizontalExpand = true,
                        Children =
                        {
                            (_bulletsList = new BoxContainer
                            {
                                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                                VerticalAlignment = VAlignment.Center,
                                SeparationOverride = 0
                            }),
                            (_noMagazineLabel = new Label
                            {
                                Text = "No Magazine!",
                                StyleClasses = {StyleNano.StyleClassItemStatus}
                            })
                        }
                    },
                    new Control() { MinSize = (5,0) },
                    (_ammoCount = new Label
                    {
                        StyleClasses = {StyleNano.StyleClassItemStatus},
                        HorizontalAlignment = HAlignment.Right,
                    }),
                }
            });
        }

        public void Update()
        {
            _chamberedBullet.ModulateSelfOverride =
                _parent.Chambered ? Color.FromHex("#d7df60") : Color.Black;

            _bulletsList.RemoveAllChildren();

            if (_parent.MagazineCount == null)
            {
                _noMagazineLabel.Visible = true;
                _ammoCount.Visible = false;
                return;
            }

            var (count, capacity) = _parent.MagazineCount.Value;

            _noMagazineLabel.Visible = false;
            _ammoCount.Visible = true;

            var texturePath = "/Textures/Interface/ItemStatus/Bullets/normal.png";
            var texture = StaticIoC.ResC.GetTexture(texturePath);

            _ammoCount.Text = $"x{count:00}";
            capacity = Math.Min(capacity, 20);
            FillBulletRow(_bulletsList, count, capacity, texture);
        }

        private static void FillBulletRow(Control container, int count, int capacity, Texture texture)
        {
            var colorA = Color.FromHex("#b68f0e");
            var colorB = Color.FromHex("#d7df60");
            var colorGoneA = Color.FromHex("#000000");
            var colorGoneB = Color.FromHex("#222222");

            var altColor = false;

            // Draw the empty ones
            for (var i = count; i < capacity; i++)
            {
                container.AddChild(new TextureRect
                {
                    Texture = texture,
                    ModulateSelfOverride = altColor ? colorGoneA : colorGoneB,
                    Stretch = TextureRect.StretchMode.KeepCentered
                });

                altColor ^= true;
            }

            // Draw the full ones, but limit the count to the capacity
            count = Math.Min(count, capacity);
            for (var i = 0; i < count; i++)
            {
                container.AddChild(new TextureRect
                {
                    Texture = texture,
                    ModulateSelfOverride = altColor ? colorA : colorB,
                    Stretch = TextureRect.StretchMode.KeepCentered
                });

                altColor ^= true;
            }
        }

        public void PlayAlarmAnimation()
        {
            var animation = _parent._isLmgAlarmAnimation ? AlarmAnimationLmg : AlarmAnimationSmg;
            _noMagazineLabel.PlayAnimation(animation, "alarm");
        }
    }
}
