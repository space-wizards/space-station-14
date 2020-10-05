#nullable enable
using Content.Client.UserInterface.Stylesheets;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;
using System;
using System.Threading.Tasks;
using Content.Client.GameObjects.Components.Mobs;
using Content.Shared.Audio;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Systems;

namespace Content.Client.GameObjects.Components.Weapons.Ranged.Barrels
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedRangedWeaponComponent))]
    public class ClientBatteryBarrelComponent : SharedBatteryBarrelComponent, IItemStatus
    {
        private StatusControl? _statusControl;

        /// <summary>
        ///     Count of bullets in the magazine.
        /// </summary>
        /// <remarks>
        ///     Null if no magazine is inserted.
        ///     Didn't call it Capacity because that's the battery capacity rather than shots left capacity like the other guns.
        /// </remarks>
        [ViewVariables]
        public (float CurrentCharge, float MaxCharge)? PowerCell { get; private set; }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (!(curState is BatteryBarrelComponentState cast))
                return;

            PowerCell = cast.PowerCell;
            _statusControl?.Update();
            UpdateAppearance();
        }
        
        public override void UpdateAppearance()
        {
            if (!Owner.TryGetComponent(out AppearanceComponent? appearanceComponent))
                return;

            var count = (int) MathF.Ceiling(PowerCell?.CurrentCharge / BaseFireCost ?? 0);
            var max = (int) MathF.Ceiling(PowerCell?.MaxCharge / BaseFireCost ?? 0);
            
            appearanceComponent.SetData(MagazineBarrelVisuals.MagLoaded, PowerCell != null);
            appearanceComponent.SetData(AmmoVisuals.AmmoCount, count);
            appearanceComponent.SetData(AmmoVisuals.AmmoMax, max);
        }

        protected override bool TryShoot(Angle angle)
        {
            if (!base.TryShoot(angle))
                return false;
            
            if (PowerCell == null)
                return false;

            var (currentCharge, maxCharge) = PowerCell.Value;
            if (currentCharge < LowerChargeLimit)
            {
                if (SoundEmpty != null)
                    EntitySystem.Get<AudioSystem>().Play(SoundEmpty, Owner, AudioHelpers.WithVariation(EmptyVariation).WithVolume(EmptyVolume));
                
                return false;
            }
            
            var chargeChange = Math.Min(currentCharge, BaseFireCost);
            PowerCell = (currentCharge - chargeChange, maxCharge);
            
            var shooter = Shooter();
            CameraRecoilComponent? cameraRecoilComponent = null;
            shooter?.TryGetComponent(out cameraRecoilComponent);

            cameraRecoilComponent?.Kick(-angle.ToVec().Normalized * RecoilMultiplier * chargeChange / BaseFireCost);
            
            if (!AmmoIsHitscan)
                EntitySystem.Get<SharedRangedWeaponSystem>().MuzzleFlash(shooter, this, angle);

            if (SoundGunshot != null)
                EntitySystem.Get<AudioSystem>().Play(SoundGunshot, Owner, AudioHelpers.WithVariation(GunshotVariation).WithVolume(GunshotVolume));
            
            // TODO: Show effect here once we can get the full hitscan predicted
            
            UpdateAppearance();
            _statusControl?.Update();
            return true;
        }

        public override async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            // TODO
            return true;
        }

        public override bool UseEntity(UseEntityEventArgs eventArgs)
        {
            // TODO
            return true;
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
            private readonly ClientBatteryBarrelComponent _parent;
            private readonly HBoxContainer _bulletsList;
            private readonly Label _noBatteryLabel;
            private readonly Label _ammoCount;

            public StatusControl(ClientBatteryBarrelComponent parent)
            {
                _parent = parent;
                SizeFlagsHorizontal = SizeFlags.FillExpand;
                SizeFlagsVertical = SizeFlags.ShrinkCenter;

                AddChild(new HBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    Children =
                    {
                        new Control
                        {
                            SizeFlagsHorizontal = SizeFlags.FillExpand,
                            Children =
                            {
                                (_bulletsList = new HBoxContainer
                                {
                                    SizeFlagsVertical = SizeFlags.ShrinkCenter,
                                    SeparationOverride = 4
                                }),
                                (_noBatteryLabel = new Label
                                {
                                    Text = "No Battery!",
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
                _bulletsList.RemoveAllChildren();

                if (_parent.PowerCell == null)
                {
                    _noBatteryLabel.Visible = true;
                    _ammoCount.Visible = false;
                    return;
                }

                var count = (int) MathF.Ceiling(_parent.PowerCell.Value.CurrentCharge / _parent.BaseFireCost);
                var max = (int) MathF.Ceiling(_parent.PowerCell.Value.MaxCharge / _parent.BaseFireCost);

                _noBatteryLabel.Visible = false;
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
                        CustomMinimumSize = (10, 15),
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
                        CustomMinimumSize = (10, 15),
                    });
                }
            }

            protected override Vector2 CalculateMinimumSize()
            {
                return Vector2.ComponentMax((0, 15), base.CalculateMinimumSize());
            }
        }
    }
}
