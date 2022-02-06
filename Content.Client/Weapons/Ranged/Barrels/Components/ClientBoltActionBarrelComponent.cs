using System;
using Content.Client.IoC;
using Content.Client.Items.Components;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Weapons.Ranged.Barrels.Components
{
    [RegisterComponent]
    [NetworkedComponent()]
    public class ClientBoltActionBarrelComponent : Component, IItemStatus
    {
        private StatusControl? _statusControl;

        /// <summary>
        ///     chambered is true when a bullet is chambered
        ///     spent is true when the chambered bullet is spent
        /// </summary>
        [ViewVariables]
        public (bool chambered, bool spent) Chamber { get; private set; }

        /// <summary>
        ///     Count of bullets in the magazine.
        /// </summary>
        /// <remarks>
        ///     Null if no magazine is inserted.
        /// </remarks>
        [ViewVariables]
        public (int count, int max)? MagazineCount { get; private set; }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

           if (curState is not BoltActionBarrelComponentState cast)
                return;

            Chamber = cast.Chamber;
            MagazineCount = cast.Magazine;
            _statusControl?.Update();
        }

        public Control MakeControl()
        {
            _statusControl = new StatusControl(this);
            _statusControl.Update();
            return _statusControl;
        }

        public void DestroyControl(Control control)
        {
            if (_statusControl == control)
            {
                _statusControl = null;
            }
        }

        private sealed class StatusControl : Control
        {
            private readonly ClientBoltActionBarrelComponent _parent;
            private readonly BoxContainer _bulletsListTop;
            private readonly BoxContainer _bulletsListBottom;
            private readonly TextureRect _chamberedBullet;
            private readonly Label _noMagazineLabel;

            public StatusControl(ClientBoltActionBarrelComponent parent)
            {
                MinHeight = 15;
                _parent = parent;
                HorizontalExpand = true;
                VerticalAlignment = VAlignment.Center;
                AddChild(new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    HorizontalExpand = true,
                    VerticalAlignment = VAlignment.Center,
                    SeparationOverride = 0,
                    Children =
                    {
                        (_bulletsListTop = new BoxContainer
                        {
                            Orientation = LayoutOrientation.Horizontal,
                            SeparationOverride = 0
                        }),
                        new BoxContainer
                        {
                            Orientation = LayoutOrientation.Horizontal,
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
                                            Orientation = LayoutOrientation.Horizontal,
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
                                (_chamberedBullet = new TextureRect
                                {
                                    Texture = StaticIoC.ResC.GetTexture("/Textures/Interface/ItemStatus/Bullets/chambered.png"),
                                    VerticalAlignment = VAlignment.Center,
                                    HorizontalAlignment = HAlignment.Right,
                                })
                            }
                        }
                    }
                });
            }

            public void Update()
            {
                _chamberedBullet.ModulateSelfOverride =
                    _parent.Chamber.chambered ?
                    _parent.Chamber.spent ? Color.Red : Color.FromHex("#d7df60")
                    : Color.Black;

                _bulletsListTop.RemoveAllChildren();
                _bulletsListBottom.RemoveAllChildren();

                if (_parent.MagazineCount == null)
                {
                    _noMagazineLabel.Visible = true;
                    return;
                }

                var (count, capacity) = _parent.MagazineCount.Value;

                _noMagazineLabel.Visible = false;

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
    }
}
