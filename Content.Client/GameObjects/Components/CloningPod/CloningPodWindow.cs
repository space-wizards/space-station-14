#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Content.Shared.GameObjects.Components.Medical.SharedCloningPodComponent;

namespace Content.Client.GameObjects.Components.CloningPod
{
    public sealed class CloningPodWindow : SS14Window
    {
        private Dictionary<int, string> _scanManager;

        private readonly VBoxContainer _mainVBox;
        private readonly ScanListContainer _scanList;
        private readonly LineEdit _searchBar;
        private readonly Button _clearButton;
        public readonly Button CloneButton;
        public readonly Button EjectButton;
        private readonly CloningScanButton _measureButton;
        private CloningScanButton? _selectedButton;
        private readonly Label _progressLabel;
        private readonly ProgressBar _cloningProgressBar;
        private readonly Label _mindState;

        protected override Vector2 ContentsMinimumSize => _mainVBox?.CombinedMinimumSize ?? Vector2.Zero;
        private CloningPodBoundUserInterfaceState _lastUpdate = null!;

        // List of scans that are visible based on current filter criteria.
        private readonly Dictionary<int, string> _filteredScans = new();

        // The indices of the visible scans last time UpdateVisibleScans was ran.
        // This is inclusive, so end is the index of the last scan, not right after it.
        private (int start, int end) _lastScanIndices;

        public int? SelectedScan;

        protected override Vector2? CustomSize => (250, 300);

        public CloningPodWindow(
            Dictionary<int, string> scanManager)
        {
            _scanManager = scanManager;


            Title = Loc.GetString("Cloning Machine");

            Contents.AddChild(_mainVBox = new VBoxContainer
            {
                Children =
                {
                    new HBoxContainer
                    {
                        Children =
                        {
                            (_searchBar = new LineEdit
                            {
                                SizeFlagsHorizontal = SizeFlags.FillExpand,
                                PlaceHolder = Loc.GetString("Search")
                            }),

                            (_clearButton = new Button
                            {
                                Disabled = true,
                                Text = Loc.GetString("Clear"),
                            })
                        }
                    },
                    new ScrollContainer
                    {
                        CustomMinimumSize = new Vector2(200.0f, 0.0f),
                        SizeFlagsVertical = SizeFlags.FillExpand,
                        Children =
                        {
                            (_scanList = new ScanListContainer())
                        }
                    },
                    new VBoxContainer
                    {
                        Children =
                        {
                            (CloneButton = new Button
                            {
                                Text = Loc.GetString("Clone")
                            })
                        }
                    },
                    (_measureButton = new CloningScanButton {Visible = false}),
                    (_cloningProgressBar = new ProgressBar
                    {
                        CustomMinimumSize = (200, 20),
                        SizeFlagsHorizontal = SizeFlags.Fill,
                        MinValue = 0,
                        MaxValue = 10,
                        Page = 0,
                        Value = 0.5f,
                        Children =
                        {
                            (_progressLabel = new Label())
                        }
                    }),
                    (EjectButton = new Button
                    {
                        Text = Loc.GetString("Eject Body")
                    }),
                    new HBoxContainer
                    {
                        Children =
                        {
                            new Label()
                            {
                                Text = Loc.GetString("Neural Interface: ")
                            },
                            (_mindState = new Label()
                            {
                                Text = Loc.GetString("No Activity"),
                                FontColorOverride = Color.Red
                            }),
                        }
                    }
                }
            });


            _searchBar.OnTextChanged += OnSearchBarTextChanged;
            _clearButton.OnPressed += OnClearButtonPressed;

            BuildEntityList();

            _searchBar.GrabKeyboardFocus();
        }

        public void Populate(CloningPodBoundUserInterfaceState state)
        {
            //Ignore useless updates or we can't interact with the UI
            //TODO: come up with a better comparision, probably write a comparator because '.Equals' doesn't work
            if (_lastUpdate == null || _lastUpdate.MindIdName.Count != state.MindIdName.Count)
            {
                _scanManager = state.MindIdName;
                BuildEntityList();
                _lastUpdate = state;
            }

            var percentage = state.Progress / _cloningProgressBar.MaxValue * 100;
            _progressLabel.Text = $"{percentage:0}%";

            _cloningProgressBar.Value = state.Progress;
            _mindState.Text = Loc.GetString(state.MindPresent ? "Consciousness Detected" : "No Activity");
            _mindState.FontColorOverride = state.MindPresent ? Color.Green : Color.Red;
        }

        private void OnSearchBarTextChanged(LineEdit.LineEditEventArgs args)
        {
            BuildEntityList(args.Text);
            _clearButton.Disabled = string.IsNullOrEmpty(args.Text);
        }

        private void OnClearButtonPressed(BaseButton.ButtonEventArgs args)
        {
            _searchBar.Clear();
            BuildEntityList("");
        }


        private void BuildEntityList(string? searchStr = null)
        {
            _filteredScans.Clear();
            _scanList.RemoveAllChildren();
            // Reset last scan indices so it automatically updates the entire list.
            _lastScanIndices = (0, -1);
            _scanList.RemoveAllChildren();
            _selectedButton = null;
            searchStr = searchStr?.ToLowerInvariant();

            foreach (var scan in _scanManager)
            {
                if (searchStr != null && !_doesScanMatchSearch(scan.Value, searchStr))
                {
                    continue;
                }

                _filteredScans.Add(scan.Key, scan.Value);
            }

            //TODO: set up sort
            //_filteredScans.Sort((a, b) => string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal));

            _scanList.TotalItemCount = _filteredScans.Count;
        }

        private void UpdateVisibleScans()
        {
            // Update visible buttons in the scan list.

            // Calculate index of first scan to render based on current scroll.
            var height = _measureButton.CombinedMinimumSize.Y + ScanListContainer.Separation;
            var offset = -_scanList.Position.Y;
            var startIndex = (int) Math.Floor(offset / height);
            _scanList.ItemOffset = startIndex;

            var (prevStart, prevEnd) = _lastScanIndices;

            // Calculate index of final one.
            var endIndex = startIndex - 1;
            var spaceUsed = -height; // -height instead of 0 because else it cuts off the last button.

            while (spaceUsed < _scanList.Parent!.Height)
            {
                spaceUsed += height;
                endIndex += 1;
            }

            endIndex = Math.Min(endIndex, _filteredScans.Count - 1);

            if (endIndex == prevEnd && startIndex == prevStart)
            {
                // Nothing changed so bye.
                return;
            }

            _lastScanIndices = (startIndex, endIndex);

            // Delete buttons at the start of the list that are no longer visible (scrolling down).
            for (var i = prevStart; i < startIndex && i <= prevEnd; i++)
            {
                var control = (CloningScanButton) _scanList.GetChild(0);
                DebugTools.Assert(control.Index == i);
                _scanList.RemoveChild(control);
            }

            // Delete buttons at the end of the list that are no longer visible (scrolling up).
            for (var i = prevEnd; i > endIndex && i >= prevStart; i--)
            {
                var control = (CloningScanButton) _scanList.GetChild(_scanList.ChildCount - 1);
                DebugTools.Assert(control.Index == i);
                _scanList.RemoveChild(control);
            }

            var array = _filteredScans.ToArray();

            // Create buttons at the start of the list that are now visible (scrolling up).
            for (var i = Math.Min(prevStart - 1, endIndex); i >= startIndex; i--)
            {
                InsertEntityButton(array[i], true, i);
            }

            // Create buttons at the end of the list that are now visible (scrolling down).
            for (var i = Math.Max(prevEnd + 1, startIndex); i <= endIndex; i++)
            {
                InsertEntityButton(array[i], false, i);
            }
        }

        // Create a spawn button and insert it into the start or end of the list.
        private void InsertEntityButton(KeyValuePair<int, string> scan, bool insertFirst, int index)
        {
            var button = new CloningScanButton
            {
                Scan = scan.Value,
                Id = scan.Key,
                Index = index // We track this index purely for debugging.
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
            if (insertFirst)
            {
                button.SetPositionInParent(0);
            }
        }

        private static bool _doesScanMatchSearch(string scan, string searchStr)
        {
            return scan.ToLowerInvariant().Contains(searchStr);
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

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            UpdateVisibleScans();
        }

        private class ScanListContainer : Container
        {
            // Quick and dirty container to do virtualization of the list.
            // Basically, get total item count and offset to put the current buttons at.
            // Get a constant minimum height and move the buttons in the list up to match the scrollbar.
            private int _totalItemCount;
            private int _itemOffset;

            public int TotalItemCount
            {
                get => _totalItemCount;
                set
                {
                    _totalItemCount = value;
                    MinimumSizeChanged();
                }
            }

            public int ItemOffset
            {
                get => _itemOffset;
                set
                {
                    _itemOffset = value;
                    UpdateLayout();
                }
            }

            public const float Separation = 2;

            protected override Vector2 CalculateMinimumSize()
            {
                if (ChildCount == 0)
                {
                    return Vector2.Zero;
                }

                var first = GetChild(0);

                var (minX, minY) = first.CombinedMinimumSize;

                return (minX, minY * TotalItemCount + (TotalItemCount - 1) * Separation);
            }

            protected override void LayoutUpdateOverride()
            {
                if (ChildCount == 0)
                {
                    return;
                }

                var first = GetChild(0);

                var height = first.CombinedMinimumSize.Y;
                var offset = ItemOffset * height + (ItemOffset - 1) * Separation;

                foreach (var child in Children)
                {
                    FitChildInBox(child, UIBox2.FromDimensions(0, offset, Width, height));
                    offset += Separation + height;
                }
            }
        }

        [DebuggerDisplay("cloningbutton {" + nameof(Index) + "}")]
        private class CloningScanButton : Control
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
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    SizeFlagsVertical = SizeFlags.FillExpand,
                    ToggleMode = true,
                });

                AddChild(new HBoxContainer
                {
                    Children =
                    {
                        (EntityTextureRect = new TextureRect
                        {
                            CustomMinimumSize = (32, 32),
                            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                            SizeFlagsVertical = SizeFlags.ShrinkCenter,
                            Stretch = TextureRect.StretchMode.KeepAspectCentered,
                            CanShrink = true
                        }),
                        (EntityLabel = new Label
                        {
                            SizeFlagsVertical = SizeFlags.ShrinkCenter,
                            SizeFlagsHorizontal = SizeFlags.FillExpand,
                            Text = "",
                            ClipText = true
                        })
                    }
                });
            }
        }
    }
}
