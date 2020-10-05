#nullable enable
using System;
using System.Collections.Generic;
using Content.Client.Animations;
using Content.Client.GameObjects.Components.Mobs;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Client.GameObjects.Components.Weapons.Ranged.Barrels
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedRangedWeaponComponent))]
    public sealed class ClientMagazineBarrelComponent : SharedMagazineBarrelComponent, IExamine, IItemStatus
    {
        private static readonly Animation AlarmAnimationSmg = new Animation
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
                        new AnimationTrackProperty.KeyFrame(Color.Transparent, 0.3f),
                        new AnimationTrackProperty.KeyFrame(Color.Red, 0.2f),
                        new AnimationTrackProperty.KeyFrame(Color.Transparent, 0.3f),
                        new AnimationTrackProperty.KeyFrame(Color.Red, 0.2f),
                        new AnimationTrackProperty.KeyFrame(Color.Transparent, 0.3f),
                    }
                }
            }
        };

        private static readonly Animation AlarmAnimationLmg = new Animation
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
                        new AnimationTrackProperty.KeyFrame(Color.Transparent, 0.15f),
                        new AnimationTrackProperty.KeyFrame(Color.Red, 0.15f),
                        new AnimationTrackProperty.KeyFrame(Color.Transparent, 0.15f),
                        new AnimationTrackProperty.KeyFrame(Color.Red, 0.15f),
                        new AnimationTrackProperty.KeyFrame(Color.Transparent, 0.15f),
                    }
                }
            }
        };
        
        private StatusControl? _statusControl;

        private bool _isLmgAlarmAnimation;
        
        private bool? _chamber;

        private bool HasMagazine => _magazine != null;
        
        private Stack<bool>? _magazine;

        private int ShotsLeft => _magazine?.Count ?? 0;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _isLmgAlarmAnimation, "isLmgAlarmAnimation", false);
        }

        private void UpdateAppearance()
        {
            if (!Owner.TryGetComponent(out AppearanceComponent? appearanceComponent))
                return;
            
            appearanceComponent.SetData(BarrelBoltVisuals.BoltOpen, BoltOpen);
            appearanceComponent.SetData(MagazineBarrelVisuals.MagLoaded, _magazine != null);
            appearanceComponent.SetData(AmmoVisuals.AmmoCount, ShotsLeft);
            appearanceComponent.SetData(AmmoVisuals.AmmoMax, Capacity);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is MagazineBarrelComponentState cast))
                return;
            
            // Don't want to use TrySetBolt in case it gets re-predicted... I think
            BoltOpen = cast.BoltOpen;
            _chamber = cast.Chambered;
            _magazine = cast.Magazine;
            UpdateAppearance();
            _statusControl?.Update();
        }

        protected override void Cycle(bool manual = false)
        {
            TryEjectChamber();
            TryFeedChamber();

            if (manual)
            {
                if (SoundRack != null)
                    EntitySystem.Get<AudioSystem>().Play(SoundRack, Owner, AudioHelpers.WithVariation(RackVariation).WithVolume(RackVolume));
            }
            
            UpdateAppearance();
            _statusControl?.Update();
        }

        protected override bool TrySetBolt(bool value)
        {
            if (BoltOpen == value)
                return false;

            if (value)
            {
                TryEjectChamber();
            }
            else
            {
                TryFeedChamber();
                // TODO: Predict sounds once we can
            }

            BoltOpen = value;
            UpdateAppearance();
            _statusControl?.Update();
            return true;
        }

        protected override void TryEjectChamber()
        {
            _chamber = null;
        }

        protected override void TryFeedChamber()
        {
            if (_chamber != null)
                return;

            // Try and pull a round from the magazine to replace the chamber if possible
            if (_magazine == null || !_magazine.TryPop(out var nextCartridge))
                return;

            _chamber = nextCartridge;

            if (AutoEjectMag && _magazine != null && _magazine.Count == 0)
            {
                if (SoundAutoEject != null)
                    EntitySystem.Get<AudioSystem>().Play(SoundAutoEject, Owner, AudioHelpers.WithVariation(AutoEjectVariation));
                
                _statusControl?.PlayAlarmAnimation();
            }
        }

        protected override void RemoveMagazine(IEntity user)
        {
            _magazine = null;
            UpdateAppearance();
            _statusControl?.Update();
        }

        protected override bool TryInsertMag(IEntity user, IEntity mag)
        {
            // TODO
            return true;
        }

        protected override bool TryInsertAmmo(IEntity user, IEntity ammo)
        {
            // TODO
            return true;
        }

        protected override bool UseEntity(IEntity user)
        {
            // TODO
            return true;
        }

        protected override bool TryShoot(Angle angle)
        {
            if (!base.TryShoot(angle))
                return false;

            var chamber = _chamber;
            Cycle();

            if (chamber == null)
            {
                if (SoundEmpty != null)
                    EntitySystem.Get<AudioSystem>().Play(SoundEmpty, Owner, AudioHelpers.WithVariation(EmptyVariation));
                
                if (!BoltOpen && (_magazine == null || _magazine.Count == 0))
                    TrySetBolt(true);

                return true;
            }

            var shooter = Shooter();
            CameraRecoilComponent? cameraRecoilComponent = null;
            shooter?.TryGetComponent(out cameraRecoilComponent);
            
            string? sound;
            float variation;

            if (chamber.Value)
            {
                sound = SoundGunshot;
                variation = GunshotVariation;
                cameraRecoilComponent?.Kick(-angle.ToVec().Normalized * RecoilMultiplier);
                EntitySystem.Get<SharedRangedWeaponSystem>().MuzzleFlash(shooter, this, angle);
            }
            else
            {
                sound = SoundEmpty;
                variation = EmptyVariation;
            }

            if (sound != null)
                EntitySystem.Get<AudioSystem>().Play(sound, Owner, AudioHelpers.WithVariation(variation));
            
            UpdateAppearance();
            _statusControl?.Update();
            return true;
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsInRange)
        {
            message.AddMarkup(Loc.GetString("\nIt uses [color=white]{0}[/color] ammo.", Caliber));

            foreach (var magazineType in GetMagazineTypes())
            {
                message.AddMarkup(Loc.GetString("\nIt accepts [color=white]{0}[/color] magazines.", magazineType));
            }
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
            private readonly HBoxContainer _bulletsList;
            private readonly TextureRect _chamberedBullet;
            private readonly Label _noMagazineLabel;
            private readonly Label _ammoCount;

            public StatusControl(ClientMagazineBarrelComponent parent)
            {
                _parent = parent;
                SizeFlagsHorizontal = SizeFlags.FillExpand;
                SizeFlagsVertical = SizeFlags.ShrinkCenter;

                AddChild(new HBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    Children =
                    {
                        (_chamberedBullet = new TextureRect
                        {
                            Texture = StaticIoC.ResC.GetTexture("/Textures/Interface/ItemStatus/Bullets/chambered_rotated.png"),
                            SizeFlagsVertical = SizeFlags.ShrinkCenter,
                            SizeFlagsHorizontal = SizeFlags.ShrinkEnd | SizeFlags.Fill,
                        }),
                        new Control() { CustomMinimumSize = (5,0) },
                        new Control
                        {
                            SizeFlagsHorizontal = SizeFlags.FillExpand,
                            Children =
                            {
                                (_bulletsList = new HBoxContainer
                                {
                                    SizeFlagsVertical = SizeFlags.ShrinkCenter,
                                    SeparationOverride = 0
                                }),
                                (_noMagazineLabel = new Label
                                {
                                    Text = "No Magazine!",
                                    StyleClasses = {StyleNano.StyleClassItemStatus}
                                })
                            }
                        },
                        new Control() { CustomMinimumSize = (5,0) },
                        (_ammoCount = new Label
                        {
                            StyleClasses = {StyleNano.StyleClassItemStatus},
                            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                        }),
                    }
                });
            }

            public void Update()
            {
                _chamberedBullet.ModulateSelfOverride =
                    _parent._chamber != null ? Color.FromHex("#d7df60") : Color.Black;

                _bulletsList.RemoveAllChildren();

                if (_parent._magazine == null)
                {
                    _noMagazineLabel.Visible = true;
                    _ammoCount.Visible = false;
                    return;
                }

                var count = _parent.ShotsLeft;
                var capacity = _parent.Capacity;

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
                        SizeFlagsHorizontal = SizeFlags.Fill,
                        SizeFlagsVertical = SizeFlags.Fill,
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
                        SizeFlagsHorizontal = SizeFlags.Fill,
                        SizeFlagsVertical = SizeFlags.Fill,
                        Stretch = TextureRect.StretchMode.KeepCentered
                    });

                    altColor ^= true;
                }
            }

            protected override Vector2 CalculateMinimumSize()
            {
                return Vector2.ComponentMax((0, 15), base.CalculateMinimumSize());
            }

            public void PlayAlarmAnimation()
            {
                var animation = _parent._isLmgAlarmAnimation ? AlarmAnimationLmg : AlarmAnimationSmg;
                _noMagazineLabel.PlayAnimation(animation, "alarm");
            }
        }
        
        [Verb]
        private sealed class EjectMagazineVerb : Verb<ClientMagazineBarrelComponent>
        {
            protected override void GetData(IEntity user, ClientMagazineBarrelComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Eject magazine");
                if (component.MagNeedsOpenBolt)
                {
                    data.Visibility = component.HasMagazine && component.BoltOpen
                        ? VerbVisibility.Visible
                        : VerbVisibility.Disabled;
                    return;
                }

                data.Visibility = component.HasMagazine ? VerbVisibility.Visible : VerbVisibility.Disabled;
            }

            protected override void Activate(IEntity user, ClientMagazineBarrelComponent component)
            {
                component.RemoveMagazine(user);
                component.SendNetworkMessage(new RemoveMagazineComponentMessage());
            }
        }

        [Verb]
        private sealed class OpenBoltVerb : Verb<ClientMagazineBarrelComponent>
        {
            protected override void GetData(IEntity user, ClientMagazineBarrelComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Bolt: open");
                data.Visibility = component.BoltOpen ? VerbVisibility.Invisible : VerbVisibility.Visible;
            }

            protected override void Activate(IEntity user, ClientMagazineBarrelComponent component)
            {
                component.TrySetBolt(true);
                component.SendNetworkMessage(new BoltChangedComponentMessage(true));
            }
        }

        [Verb]
        private sealed class CloseBoltVerb : Verb<ClientMagazineBarrelComponent>
        {
            protected override void GetData(IEntity user, ClientMagazineBarrelComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Bolt: close");
                data.Visibility = component.BoltOpen ? VerbVisibility.Visible : VerbVisibility.Invisible;
            }

            protected override void Activate(IEntity user, ClientMagazineBarrelComponent component)
            {
                component.TrySetBolt(false);
                component.SendNetworkMessage(new BoltChangedComponentMessage(false));
            }
        }
    }
}