#nullable enable
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using System;
using System.Collections.Generic;
using Content.Client.GameObjects.Components.Mobs;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Client.GameObjects.Components.Weapons.Ranged.Barrels
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedRangedWeaponComponent))]
    [ComponentReference(typeof(SharedBoltActionBarrelComponent))]
    public class ClientBoltActionBarrelComponent : SharedBoltActionBarrelComponent, IExamine, IItemStatus
    {

        private bool? _chamber;
        private Stack<bool?> _ammo = new Stack<bool?>();

        private StatusControl? _statusControl;
        
        /// <summary>
        ///     Not including chamber
        /// </summary>
        private int ShotsLeft => _ammo.Count + UnspawnedCount;

        public override void Initialize()
        {
            base.Initialize();
            UpdateAppearance();
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
           if (!(curState is BoltActionBarrelComponentState cast))
                return;
           
           _chamber = cast.Chamber;
           _ammo = cast.Bullets;
           SetBolt(cast.BoltOpen);
           UpdateAppearance();
            _statusControl?.Update();
        }

        protected override void SetBolt(bool value)
        {
            if (BoltOpen == value)
                return;

            if (value)
            {
                TryEjectChamber();
                if (SoundBoltOpen != null)
                {
                    EntitySystem.Get<AudioSystem>().Play(SoundBoltOpen, Owner, AudioHelpers.WithVariation(BoltToggleVariation).WithVolume(BoltToggleVolume));
                }
            }
            else
            {
                TryFeedChamber();
                if (SoundBoltClosed != null)
                {
                    EntitySystem.Get<AudioSystem>().Play(SoundBoltClosed, Owner, AudioHelpers.WithVariation(BoltToggleVariation).WithVolume(BoltToggleVolume));
                }
            }

            BoltOpen = value;
            UpdateAppearance();
            _statusControl?.Update();
        }

        private void UpdateAppearance()
        {
            if (!Owner.TryGetComponent(out AppearanceComponent? appearanceComponent))
            {
                return;
            }
            
            appearanceComponent.SetData(BarrelBoltVisuals.BoltOpen, BoltOpen);
            appearanceComponent.SetData(AmmoVisuals.AmmoCount, ShotsLeft + (_chamber != null ? 1 : 0));
            appearanceComponent.SetData(AmmoVisuals.AmmoMax, (int) Capacity);
        }

        protected override bool TryShoot(Angle angle)
        {
            if (!base.TryShoot(angle))
                return false;

            var chamber = _chamber;
            
            if (AutoCycle)
                Cycle();

            if (chamber == null)
                return true;
            
            var shooter = Shooter();
            CameraRecoilComponent? cameraRecoilComponent = null;
            shooter?.TryGetComponent(out cameraRecoilComponent);
            
            string? sound;
            float variation;
            float volume;

            if (chamber.Value)
            {
                sound = SoundGunshot;
                variation = GunshotVariation;
                volume = GunshotVolume;
                cameraRecoilComponent?.Kick(-angle.ToVec().Normalized * RecoilMultiplier);
                EntitySystem.Get<SharedRangedWeaponSystem>().MuzzleFlash(shooter, this, angle);
                if (!AutoCycle)
                    _chamber = false;
                
            }
            else
            {
                sound = SoundEmpty;
                variation = EmptyVariation;
                volume = EmptyVolume;
            }

            if (sound != null)
                EntitySystem.Get<AudioSystem>().Play(sound, Owner, AudioHelpers.WithVariation(variation).WithVolume(volume));

            UpdateAppearance();
            _statusControl?.Update();
            return true;
        }

        protected override void Cycle(bool manual = false)
        {
            TryEjectChamber();
            TryFeedChamber();
            var shooter = Shooter();

            if (_chamber == null && manual)
            {
                SetBolt(true);
                if (shooter != null)
                {
                    Owner.PopupMessage(shooter, Loc.GetString("Bolt opened"));
                }
                return;
            }
        }

        public override bool TryInsertBullet(IEntity user, SharedAmmoComponent ammoComponent)
        {
            // TODO
            return true;
        }

        protected override void TryEjectChamber()
        {
            _chamber = null;
        }

        protected override void TryFeedChamber()
        {
            if (_ammo.TryPop(out var ammo))
            {
                _chamber = ammo;
                if (SoundRack != null)
                {
                    EntitySystem.Get<AudioSystem>().Play(SoundRack, Owner, AudioHelpers.WithVariation(CycleVariation).WithVolume(CycleVolume));
                }
                
                return;
            }

            if (UnspawnedCount > 0)
            {
                _chamber = true;
                if (SoundRack != null)
                {
                    EntitySystem.Get<AudioSystem>().Play(SoundRack, Owner, AudioHelpers.WithVariation(CycleVariation).WithVolume(CycleVolume));
                }
                
                UnspawnedCount--;
            }
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString("\nIt uses [color=white]{0}[/color] ammo.", Caliber));
        }

        [Verb]
        private sealed class OpenBoltVerb : Verb<ClientBoltActionBarrelComponent>
        {
            protected override void GetData(IEntity user, ClientBoltActionBarrelComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user) || component.Shooter() != user)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Bolt: open");
                data.Visibility = component.BoltOpen ? VerbVisibility.Invisible : VerbVisibility.Visible;
            }

            protected override void Activate(IEntity user, ClientBoltActionBarrelComponent component)
            {
                component.SetBolt(true);
                component.SendNetworkMessage(new BoltChangedComponentMessage(component.BoltOpen));
            }
        }

        [Verb]
        private sealed class CloseBoltVerb : Verb<ClientBoltActionBarrelComponent>
        {
            protected override void GetData(IEntity user, ClientBoltActionBarrelComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user) || component.Shooter() != user)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Bolt: close");
                data.Visibility = component.BoltOpen ? VerbVisibility.Visible : VerbVisibility.Invisible;
            }

            protected override void Activate(IEntity user, ClientBoltActionBarrelComponent component)
            {
                component.SetBolt(false);
                component.SendNetworkMessage(new BoltChangedComponentMessage(component.BoltOpen));
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
            private readonly ClientBoltActionBarrelComponent _parent;
            private readonly HBoxContainer _bulletsListTop;
            private readonly HBoxContainer _bulletsListBottom;
            private readonly TextureRect _chamberedBullet;
            private readonly Label _noMagazineLabel;

            public StatusControl(ClientBoltActionBarrelComponent parent)
            {
                _parent = parent;
                SizeFlagsHorizontal = SizeFlags.FillExpand;
                SizeFlagsVertical = SizeFlags.ShrinkCenter;
                AddChild(new VBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    SizeFlagsVertical = SizeFlags.ShrinkCenter,
                    SeparationOverride = 0,
                    Children =
                    {
                        (_bulletsListTop = new HBoxContainer {SeparationOverride = 0}),
                        new HBoxContainer
                        {
                            SizeFlagsHorizontal = SizeFlags.FillExpand,
                            Children =
                            {
                                new Control
                                {
                                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                                    Children =
                                    {
                                        (_bulletsListBottom = new HBoxContainer
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
                                (_chamberedBullet = new TextureRect
                                {
                                    Texture = StaticIoC.ResC.GetTexture("/Textures/Interface/ItemStatus/Bullets/chambered.png"),
                                    SizeFlagsVertical = SizeFlags.ShrinkCenter,
                                    SizeFlagsHorizontal = SizeFlags.ShrinkEnd | SizeFlags.Fill,
                                })
                            }
                        }
                    }
                });
            }

            public void Update()
            {
                _chamberedBullet.ModulateSelfOverride =
                    _parent._chamber != null ?
                    !_parent._chamber.Value ? Color.Red : Color.FromHex("#d7df60")
                    : Color.Black;

                _bulletsListTop.RemoveAllChildren();
                _bulletsListBottom.RemoveAllChildren();

                var count = _parent.ShotsLeft;
                // Excluding chamber
                var capacity = _parent.Capacity - 1;

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

            protected override Vector2 CalculateMinimumSize()
            {
                return Vector2.ComponentMax((0, 15), base.CalculateMinimumSize());
            }
        }
    }
}
