using System;
using System.Collections.Generic;
using System.Diagnostics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using static Content.Shared.Cloning.SharedCloningPodComponent;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Cloning.UI
{
    public sealed class CloningPodWindow : DefaultWindow
    {
        private Dictionary<int, string?> _scanManager;

        private readonly BoxContainer _scanList;
        public readonly Button CloneButton;
        public readonly Button EjectButton;
        private CloningScanButton? _selectedButton;
        private readonly Label _progressLabel;
        private readonly ProgressBar _cloningProgressBar;
        private readonly Label _mindState;

        private CloningPodBoundUserInterfaceState? _lastUpdate;

        public int? SelectedScan;

        public CloningPodWindow(Dictionary<int, string?> scanManager)
        {
            SetSize = MinSize = (250, 300);
            _scanManager = scanManager;

            Title = Loc.GetString("cloning-pod-window-title");

            Contents.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Children =
                {
                    new ScrollContainer
                    {
                        MinSize = new Vector2(200.0f, 0.0f),
                        VerticalExpand = true,
                        Children =
                        {
                            (_scanList = new BoxContainer
                            {
                                Orientation = LayoutOrientation.Vertical
                            })
                        }
                    },
                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical,
                        Children =
                        {
                            (CloneButton = new Button
                            {
                                Text = Loc.GetString("cloning-pod-clone-button")
                            })
                        }
                    },
                    (_cloningProgressBar = new ProgressBar
                    {
                        MinSize = (200, 20),
                        MinValue = 0,
                        MaxValue = 120,
                        Page = 0,
                        Value = 0.5f,
                        Children =
                        {
                            (_progressLabel = new Label())
                        }
                    }),
                    (EjectButton = new Button
                    {
                        Text = Loc.GetString("cloning-pod-eject-body-button")
                    }),
                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Horizontal,
                        Children =
                        {
                            new Label()
                            {
                                Text = Loc.GetString($"{Loc.GetString("cloning-pod-neural-interface-label")} ")
                            },
                            (_mindState = new Label()
                            {
                                Text = Loc.GetString("cloning-pod-no-activity-text"),
                                FontColorOverride = Color.Red
                            }),
                        }
                    }
                }
            });

            BuildCloneList();
        }

        public void Populate(CloningPodBoundUserInterfaceState state)
        {
            //Ignore useless updates or we can't interact with the UI
            //TODO: come up with a better comparision, probably write a comparator because '.Equals' doesn't work
            if (_lastUpdate == null || _lastUpdate.MindIdName.Count != state.MindIdName.Count)
            {
                _scanManager = state.MindIdName;
                BuildCloneList();
            }
            _lastUpdate = state;

            _cloningProgressBar.MaxValue = state.Maximum;
            UpdateProgress();
            _mindState.Text = Loc.GetString(state.MindPresent ? "cloning-pod-mind-present-text" : "cloning-pod-no-activity-text");
            _mindState.FontColorOverride = state.MindPresent ? Color.LimeGreen : Color.Red;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            UpdateProgress();
        }

        private void UpdateProgress()
        {
            if (_lastUpdate == null)
                return;
            float simulatedProgress = _lastUpdate.Progress;
            if (_lastUpdate.Progressing)
            {
                TimeSpan sinceReference = IoCManager.Resolve<IGameTiming>().CurTime - _lastUpdate.ReferenceTime;
                simulatedProgress += (float) sinceReference.TotalSeconds;
                simulatedProgress = MathHelper.Clamp(simulatedProgress, 0f, _lastUpdate.Maximum);
            }
            var percentage = simulatedProgress / _cloningProgressBar.MaxValue * 100;
            _progressLabel.Text = $"{percentage:0}%";
            _cloningProgressBar.Value = simulatedProgress;
        }

        private void BuildCloneList()
        {
            _scanList.RemoveAllChildren();
            _selectedButton = null;

            foreach (var scan in _scanManager)
            {
                var button = new CloningScanButton
                {
                    Scan = scan.Value ?? string.Empty,
                    Id = scan.Key
                };
                button.ActualButton.OnToggled += OnItemButtonToggled;
                var entityLabelText = scan.Value;

                button.EntityLabel.Text = entityLabelText;

                if (scan.Key == SelectedScan)
                {
                    _selectedButton = button;
                    _selectedButton.ActualButton.Pressed = true;
                }

                //TODO: replace with body's face
                /*var tex = IconComponent.GetScanIcon(scan, resourceCache);
            var rect = button.EntityTextureRect;
            if (tex != null)
            {
                rect.Texture = tex.Default;
            }
            else
            {
                rect.Dispose();
            }

            rect.Dispose();
            */

                _scanList.AddChild(button);
            }

            //TODO: set up sort
            //_filteredScans.Sort((a, b) => string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal));
        }

        private void OnItemButtonToggled(BaseButton.ButtonToggledEventArgs args)
        {
            var item = (CloningScanButton) args.Button.Parent!;
            if (_selectedButton == item)
            {
                _selectedButton = null;
                SelectedScan = null;
                return;
            }
            else if (_selectedButton != null)
            {
                _selectedButton.ActualButton.Pressed = false;
            }

            _selectedButton = null;
            SelectedScan = null;

            _selectedButton = item;
            SelectedScan = item.Id;
        }

        [DebuggerDisplay("cloningbutton {" + nameof(Index) + "}")]
        private sealed class CloningScanButton : Control
        {
            public string Scan { get; set; } = default!;
            public int Id { get; set; }
            public Button ActualButton { get; private set; }
            public Label EntityLabel { get; private set; }
            public TextureRect EntityTextureRect { get; private set; }
            public int Index { get; set; }

            public CloningScanButton()
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
