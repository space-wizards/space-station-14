using Content.Client.IoC;
using Content.Client.Items;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.Weapons.Ranged.Components;
using Robust.Client.Animations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem
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
    private void RefreshControl(EntityUid uid, AmmoCounterComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        component.Control?.Dispose();
        component.Control = null;

        var ev = new AmmoCounterControlEvent();
        RaiseLocalEvent(uid, ev, false);

        // Fallback to default if none specified
        ev.Control ??= new DefaultStatusControl();

        component.Control = ev.Control;
        UpdateAmmoCount(uid, component);
    }

    private void UpdateAmmoCount(EntityUid uid, AmmoCounterComponent component)
    {
        if (component.Control == null)
            return;

        var ev = new UpdateAmmoCounterEvent()
        {
            Control = component.Control
        };

        RaiseLocalEvent(uid, ev, false);
    }

    protected override void UpdateAmmoCount(EntityUid uid)
    {
        // Don't use resolves because the method is shared and there's no compref and I'm trying to
        // share as much code as possible
        if (!Timing.IsFirstTimePredicted ||
            !TryComp<AmmoCounterComponent>(uid, out var clientComp))
        {
            return;
        }

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

    private sealed class ChamberMagazineStatusControl : Control
    {
        private readonly BoxContainer _bulletsList;
        private readonly TextureRect _chamberedBullet;
        private readonly Label _noMagazineLabel;
        private readonly Label _ammoCount;

        public ChamberMagazineStatusControl()
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

        public void Update(bool chambered, bool magazine, int count, int capacity)
        {
            _chamberedBullet.ModulateSelfOverride =
                chambered ? Color.FromHex("#d7df60") : Color.Black;

            _bulletsList.RemoveAllChildren();

            if (!magazine)
            {
                _noMagazineLabel.Visible = true;
                _ammoCount.Visible = false;
                return;
            }

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

        public void PlayAlarmAnimation(Animation animation)
        {
            _noMagazineLabel.PlayAnimation(animation, "alarm");
        }
    }

    private sealed class RevolverStatusControl : Control
    {
        private readonly BoxContainer _bulletsList;

        public RevolverStatusControl()
        {
            MinHeight = 15;
            HorizontalExpand = true;
            VerticalAlignment = VAlignment.Center;
            AddChild((_bulletsList = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                VerticalAlignment = VAlignment.Center,
                SeparationOverride = 0
            }));
        }

        public void Update(int currentIndex, bool?[] bullets)
        {
            _bulletsList.RemoveAllChildren();
            var capacity = bullets.Length;

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
            var spentTexture = StaticIoC.ResC.GetTexture("/Textures/Interface/ItemStatus/Bullets/empty.png");

            FillBulletRow(currentIndex, bullets, _bulletsList, texture, spentTexture);
        }

        private void FillBulletRow(int currentIndex, bool?[] bullets, Control container, Texture texture, Texture emptyTexture)
        {
            var capacity = bullets.Length;
            var colorA = Color.FromHex("#b68f0e");
            var colorB = Color.FromHex("#d7df60");
            var colorSpentA = Color.FromHex("#b50e25");
            var colorSpentB = Color.FromHex("#d3745f");
            var colorGoneA = Color.FromHex("#000000");
            var colorGoneB = Color.FromHex("#222222");

            var altColor = false;
            var scale = 1.3f;

            for (var i = 0; i < capacity; i++)
            {
                var bulletFree = bullets[i];
                // Add a outline
                var box = new Control()
                {
                    MinSize = texture.Size * scale,
                };
                if (i == currentIndex)
                {
                    box.AddChild(new TextureRect
                    {
                        Texture = texture,
                        TextureScale = (scale, scale),
                        ModulateSelfOverride = Color.LimeGreen,
                    });
                }
                Color color;
                Texture bulletTexture = texture;

                if (bulletFree.HasValue)
                {
                    if (bulletFree.Value)
                    {
                        color = altColor ? colorA : colorB;
                    }
                    else
                    {
                        color = altColor ? colorSpentA : colorSpentB;
                        bulletTexture = emptyTexture;
                    }
                }
                else
                {
                    color = altColor ? colorGoneA : colorGoneB;
                }

                box.AddChild(new TextureRect
                {
                    Stretch = TextureRect.StretchMode.KeepCentered,
                    Texture = bulletTexture,
                    ModulateSelfOverride = color,
                });
                altColor ^= true;
                container.AddChild(box);
            }
        }
    }

    #endregion
}
