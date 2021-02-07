using System;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Weapons.Ranged.Barrels
{
    [RegisterComponent]
    public class ClientBatteryBarrelComponent : Component, IItemStatus
    {
        public override string Name => "BatteryBarrel";
        public override uint? NetID => ContentNetIDs.BATTERY_BARREL;

        private StatusControl _statusControl;

        /// <summary>
        ///     Count of bullets in the magazine.
        /// </summary>
        /// <remarks>
        ///     Null if no magazine is inserted.
        /// </remarks>
        [ViewVariables]
        public (int count, int max)? MagazineCount { get; private set; }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not BatteryBarrelComponentState cast)
                return;

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

                if (_parent.MagazineCount == null)
                {
                    _noBatteryLabel.Visible = true;
                    _ammoCount.Visible = false;
                    return;
                }

                var (count, capacity) = _parent.MagazineCount.Value;

                _noBatteryLabel.Visible = false;
                _ammoCount.Visible = true;

                _ammoCount.Text = $"x{count:00}";
                capacity = Math.Min(capacity, 8);
                FillBulletRow(_bulletsList, count, capacity);
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
