using Content.Client.Items.Components;
using Content.Client.Stylesheets;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameStates;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Weapons.Ranged.Barrels.Components
{
    [RegisterComponent]
    [NetworkedComponent()]
    public sealed class ClientBatteryBarrelComponent : Component, IItemStatus
    {
        public StatusControl? ItemStatus;

        public Control MakeControl()
        {
            ItemStatus = new StatusControl(this);

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out AppearanceComponent appearance))
                ItemStatus.Update(appearance);

            return ItemStatus;
        }

        public void DestroyControl(Control control)
        {
            if (ItemStatus == control)
            {
                ItemStatus = null;
            }
        }

        public sealed class StatusControl : Control
        {
            private readonly ClientBatteryBarrelComponent _parent;
            private readonly BoxContainer _bulletsList;
            private readonly Label _noBatteryLabel;
            private readonly Label _ammoCount;

            public StatusControl(ClientBatteryBarrelComponent parent)
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
                        new Control
                        {
                            HorizontalExpand = true,
                            Children =
                            {
                                (_bulletsList = new BoxContainer
                                {
                                    Orientation = LayoutOrientation.Horizontal,
                                    VerticalAlignment = VAlignment.Center,
                                    SeparationOverride = 4
                                }),
                                (_noBatteryLabel = new Label
                                {
                                    Text = "No Battery!",
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

            public void Update(AppearanceComponent appearance)
            {
                _bulletsList.RemoveAllChildren();

                if (!appearance.TryGetData(MagazineBarrelVisuals.MagLoaded, out bool loaded) || !loaded)
                {
                    _noBatteryLabel.Visible = true;
                    _ammoCount.Visible = false;
                    return;
                }

                appearance.TryGetData(AmmoVisuals.AmmoCount, out int count);
                appearance.TryGetData(AmmoVisuals.AmmoMax, out int capacity);

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
    }
}
