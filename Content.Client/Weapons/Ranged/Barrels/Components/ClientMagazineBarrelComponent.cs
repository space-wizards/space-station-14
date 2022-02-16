using System;
using Content.Client.IoC;
using Content.Client.Items.Components;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Client.Animations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Weapons.Ranged.Barrels.Components
{
    [RegisterComponent]
    [NetworkedComponent()]
    public sealed class ClientMagazineBarrelComponent : Component, IItemStatus
    {
        private static readonly Animation AlarmAnimationSmg = new()
        {
            Length = TimeSpan.FromSeconds(1.4),
            AnimationTracks =
            {
                new AnimationTrackControlProperty
                {
                    // These timings match the SMG audio file.
                    Property = nameof(Label.FontColorOverride),
                    InterpolationMode = AnimationInterpolationMode.Previous,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Color.Red, 0.1f),
                        new AnimationTrackProperty.KeyFrame(null!, 0.3f),
                        new AnimationTrackProperty.KeyFrame(Color.Red, 0.2f),
                        new AnimationTrackProperty.KeyFrame(null!, 0.3f),
                        new AnimationTrackProperty.KeyFrame(Color.Red, 0.2f),
                        new AnimationTrackProperty.KeyFrame(null!, 0.3f),
                    }
                }
            }
        };

        private static readonly Animation AlarmAnimationLmg = new()
        {
            Length = TimeSpan.FromSeconds(0.75),
            AnimationTracks =
            {
                new AnimationTrackControlProperty
                {
                    // These timings match the SMG audio file.
                    Property = nameof(Label.FontColorOverride),
                    InterpolationMode = AnimationInterpolationMode.Previous,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Color.Red, 0.0f),
                        new AnimationTrackProperty.KeyFrame(null!, 0.15f),
                        new AnimationTrackProperty.KeyFrame(Color.Red, 0.15f),
                        new AnimationTrackProperty.KeyFrame(null!, 0.15f),
                        new AnimationTrackProperty.KeyFrame(Color.Red, 0.15f),
                        new AnimationTrackProperty.KeyFrame(null!, 0.15f),
                    }
                }
            }
        };
        private StatusControl? _statusControl;

        /// <summary>
        ///     True if a bullet is chambered.
        /// </summary>
        [ViewVariables]
        public bool Chambered { get; private set; }

        /// <summary>
        ///     Count of bullets in the magazine.
        /// </summary>
        /// <remarks>
        ///     Null if no magazine is inserted.
        /// </remarks>
        [ViewVariables]
        public (int count, int max)? MagazineCount { get; private set; }

        [ViewVariables(VVAccess.ReadWrite)] [DataField("lmg_alarm_animation")] private bool _isLmgAlarmAnimation = default;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not MagazineBarrelComponentState cast)
                return;

            Chambered = cast.Chambered;
            MagazineCount = cast.Magazine;
            _statusControl?.Update();
        }

        public void PlayAlarmAnimation()
        {
            _statusControl?.PlayAlarmAnimation();
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
            private readonly ClientMagazineBarrelComponent _parent;
            private readonly BoxContainer _bulletsList;
            private readonly TextureRect _chamberedBullet;
            private readonly Label _noMagazineLabel;
            private readonly Label _ammoCount;

            public StatusControl(ClientMagazineBarrelComponent parent)
            {
                MinHeight = 15;
                _parent = parent;
                HorizontalExpand = true;
                VerticalAlignment = VAlignment.Center;

                AddChild(new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
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
}
