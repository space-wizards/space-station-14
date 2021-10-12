using System;
using System.Collections.Generic;
using System.Diagnostics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using static Content.Shared.Storage.SharedSuitStorageComponent;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Storage.UI
{
    public sealed class SuitStorageWindow : SS14Window
    {
        private Dictionary<int, string?> _contentsManager;
        private readonly BoxContainer _contentsList;
        private SuitStorageBoundUserInterfaceState? _lastUpdate;
        private SuitStorageButton? _selectedButton;
        private bool _powered;
        public readonly Button OpenStorageButton;
        public readonly Button CloseStorageButton;
        public readonly Button DispenseButton;
        public readonly Label PowerLabel;
        public int? SelectedItem;

        public SuitStorageWindow(Dictionary<int, string?> contentsManager)
        {
            SetSize = MinSize = (250, 300);

            _contentsManager = contentsManager;

            //TODO change to suit storage
            Title = Loc.GetString("suit-storage-window-title");

            Contents.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Children =
                {
                    (PowerLabel = new Label(){
                        Text = Loc.GetString("suit-storage-power-label")
                    }),
                    (OpenStorageButton = new Button
                    {
                        Text = Loc.GetString("suit-storage-open-button")
                    }),
                    (CloseStorageButton = new Button
                    {
                        Text = Loc.GetString("suit-storage-close-button")
                    }),
                    (DispenseButton = new Button
                    {
                        Text = Loc.GetString("suit-storage-dispense-button")
                    }),
                    new Label(){
                        Text = Loc.GetString("suit-storage-stored-items-label")
                    },
                    new ScrollContainer
                    {
                        MinSize = new Vector2(200.0f, 0.0f),
                        VerticalExpand = true,
                        Children =
                        {
                            (_contentsList = new BoxContainer
                            {
                                Orientation = LayoutOrientation.Vertical
                            })
                        }
                    }
                }
            });

            BuildStorageList();
        }

        public void Populate(SuitStorageBoundUserInterfaceState state)
        {
            _powered = state.Powered;
            _lastUpdate = state;
            //Ignore useless updates or we can't interact with the UI
            //TODO: come up with a better comparision, probably write a comparator because '.Equals' doesn't work
            if (_lastUpdate == null || _lastUpdate.Contents.Count != state.Contents.Count) return;
            _contentsManager = state.Contents;
            BuildStorageList();
            BuildPowerDisplay();
        }

        private void BuildStorageList()
        {
            _contentsList.RemoveAllChildren();
            _selectedButton = null;

            foreach (var item in _contentsManager)
            {
                var button = new SuitStorageButton
                {
                    Item = item.Value ?? string.Empty,
                    Id = item.Key
                };
                button.ActualButton.OnToggled += OnItemButtonToggled;
                var entityLabelText = item.Value;

                button.EntityLabel.Text = entityLabelText;

                if (item.Key == SelectedItem)
                {
                    _selectedButton = button;
                    _selectedButton.ActualButton.Pressed = true;
                }

                _contentsList.AddChild(button);
            }
        }

        private void BuildPowerDisplay()
        {
            PowerLabel.Text = Loc.GetString("suit-storage-power-label") + " " +
            (_powered ? Loc.GetString("suit-storage-powered") : Loc.GetString("suit-storage-unpowered"));
        }

        private void OnItemButtonToggled(BaseButton.ButtonToggledEventArgs args)
        {
            var item = (SuitStorageButton) args.Button.Parent!;
            if (_selectedButton == item)
            {
                _selectedButton = null;
                SelectedItem = null;
                return;
            }
            else if (_selectedButton != null)
            {
                _selectedButton.ActualButton.Pressed = false;
            }

            _selectedButton = null;
            SelectedItem = null;

            _selectedButton = item;
            SelectedItem = item.Id;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
        }

        [DebuggerDisplay("cloningbutton {" + nameof(Index) + "}")]
        private class SuitStorageButton : Control
        {
            public string Item { get; set; } = default!;
            public int Id { get; set; }
            public Button ActualButton { get; private set; }
            public Label EntityLabel { get; private set; }
            public TextureRect EntityTextureRect { get; private set; }
            public int Index { get; set; }

            public SuitStorageButton()
            {
                AddChild(ActualButton = new Button
                {
                    HorizontalExpand = true,
                    VerticalExpand = true,
                    ToggleMode = true,
                });

                AddChild(new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Children =
                    {
                        (EntityTextureRect = new TextureRect
                        {
                            MinSize = (32, 32),
                            HorizontalAlignment = HAlignment.Center,
                            VerticalAlignment = VAlignment.Center,
                            Stretch = TextureRect.StretchMode.KeepAspectCentered,
                            CanShrink = true
                        }),
                        (EntityLabel = new Label
                        {
                            VerticalAlignment = VAlignment.Center,
                            HorizontalExpand = true,
                            Text = string.Empty,
                            ClipText = true
                        })
                    }
                });
            }
        }
    }
}
