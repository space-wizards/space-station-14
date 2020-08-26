using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Content.Shared.GameObjects.Components.Medical.SharedCloningMachineComponent;

namespace Content.Client.GameObjects.Components.CloningMachine
{
    public sealed class CloningMachineWindow : SS14Window
    {
        //Todo: delete if we don't need
        //private readonly IScanManager scanManager;
        //private readonly IResourceCache resourceCache;
        private readonly ILocalizationManager _loc;

        private VBoxContainer MainVBox;
        private ScanListContainer ScanList;
        private Dictionary<int,string> scanManager;
        private LineEdit SearchBar;
        private OptionButton OverrideMenu;
        private Button ClearButton;
        private Button EraseButton;
        public Button CloneButton;
        private CloningScanButton MeasureButton;
        protected override Vector2 ContentsMinimumSize => MainVBox?.CombinedMinimumSize ?? Vector2.Zero;

        // List of scans that are visible based on current filter criteria.
        private readonly Dictionary<int,string> _filteredScans = new Dictionary<int,string>();

        // The indices of the visible scans last time UpdateVisibleScans was ran.
        // This is inclusive, so end is the index of the last scan, not right after it.
        private (int start, int end) _lastScanIndices;

        private CloningScanButton? SelectedButton;
        public int? SelectedScan;

        protected override Vector2? CustomSize => (250, 300);

        public CloningMachineWindow(
            Dictionary<int,string> scanManager,
            ILocalizationManager loc)
        {
            this.scanManager = scanManager;

            _loc = loc;

            Title = _loc.GetString("Cloning Machine");

            Contents.AddChild(MainVBox = new VBoxContainer
            {
                Children =
                {
                    new HBoxContainer
                    {
                        Children =
                        {
                            (SearchBar = new LineEdit
                            {
                                SizeFlagsHorizontal = SizeFlags.FillExpand,
                                PlaceHolder = _loc.GetString("Search")
                            }),

                            (ClearButton = new Button
                            {
                                Disabled = true,
                                Text = _loc.GetString("Clear"),
                            })
                        }
                    },
                    new ScrollContainer
                    {
                        CustomMinimumSize = new Vector2(200.0f, 0.0f),
                        SizeFlagsVertical = SizeFlags.FillExpand,
                        Children =
                        {
                            (ScanList = new ScanListContainer())
                        }
                    },
                    new VBoxContainer
                    {
                        Children =
                        {
                            (CloneButton = new Button
                            {
                                Text = "Clone"
                            })
                        }
                    },
                    (MeasureButton = new CloningScanButton {Visible = false})
                }
            });


            SearchBar.OnTextChanged += OnSearchBarTextChanged;
            ClearButton.OnPressed += OnClearButtonPressed;

            BuildEntityList();

            SearchBar.GrabKeyboardFocus();
        }

        public void Populate(CloningMachineBoundUserInterfaceState state)
        {
            scanManager = state.MindIdName;
            BuildEntityList();
        }

        public override void Close()
        {
            base.Close();

            Dispose();
        }


        private void OnSearchBarTextChanged(LineEdit.LineEditEventArgs args)
        {
            BuildEntityList(args.Text);
            ClearButton.Disabled = string.IsNullOrEmpty(args.Text);
        }

        /*private void OnOverrideMenuItemSelected(OptionButton.ItemSelectedEventArgs args)
        {
            OverrideMenu.SelectId(args.Id);
        }*/

        private void OnClearButtonPressed(BaseButton.ButtonEventArgs args)
        {
            SearchBar.Clear();
            BuildEntityList("");
        }


        private void BuildEntityList(string? searchStr = null)
        {
            _filteredScans.Clear();
            ScanList.RemoveAllChildren();
            // Reset last scan indices so it automatically updates the entire list.
            _lastScanIndices = (0, -1);
            ScanList.RemoveAllChildren();
            SelectedButton = null;
            searchStr = searchStr?.ToLowerInvariant();

            foreach (var scan in scanManager)
            {
                if (searchStr != null && !_doesScanMatchSearch(scan.Value, searchStr))
                {
                    continue;
                }

                _filteredScans.Add(scan.Key,scan.Value);
            }

            //TODO:Sort when we use filteredScans for a thin
            //_filteredScans.Sort((a, b) => string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal));

            ScanList.TotalItemCount = _filteredScans.Count;
        }

        private void UpdateVisibleScans()
        {
            // Update visible buttons in the scan list.

            // Calculate index of first scan to render based on current scroll.
            var height = MeasureButton.CombinedMinimumSize.Y + ScanListContainer.Separation;
            var offset = -ScanList.Position.Y;
            var startIndex = (int) Math.Floor(offset / height);
            ScanList.ItemOffset = startIndex;

            var (prevStart, prevEnd) = _lastScanIndices;

            // Calculate index of final one.
            var endIndex = startIndex - 1;
            var spaceUsed = -height; // -height instead of 0 because else it cuts off the last button.

            while (spaceUsed < ScanList.Parent!.Height)
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
                var control = (CloningScanButton) ScanList.GetChild(0);
                DebugTools.Assert(control.Index == i);
                ScanList.RemoveChild(control);
            }

            // Delete buttons at the end of the list that are no longer visible (scrolling up).
            for (var i = prevEnd; i > endIndex && i >= prevStart; i--)
            {
                var control = (CloningScanButton) ScanList.GetChild(ScanList.ChildCount - 1);
                DebugTools.Assert(control.Index == i);
                ScanList.RemoveChild(control);
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
        private void InsertEntityButton(KeyValuePair<int,string> scan, bool insertFirst, int index)
        {
            var button = new CloningScanButton
            {
                Scan = scan.Value,
                Id = scan.Key,
                Index = index // We track this index purely for debugging.
            };
            button.ActualButton.OnToggled += OnItemButtonToggled;
            var entityLabelText = scan.ToString();

            button.EntityLabel.Text = entityLabelText;

            if (scan.Key == SelectedScan)
            {
                SelectedButton = button;
                SelectedButton.ActualButton.Pressed = true;
            }

            //TODO: replace with body's face?
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

            ScanList.AddChild(button);
            if (insertFirst)
            {
                button.SetPositionInParent(0);
            }
        }

        private static bool _doesScanMatchSearch(string scan, string searchStr)
        {
            return scan.ToLowerInvariant().Contains(searchStr);
        }

        //TODO: Here is were we have the item toggle logic
        private void OnItemButtonToggled(BaseButton.ButtonToggledEventArgs args)
        {
            var item = (CloningScanButton) args.Button.Parent!;
            if (SelectedButton == item)
            {
                SelectedButton = null;
                SelectedScan = null;
                return;
            }
            else if (SelectedButton != null)
            {
                SelectedButton.ActualButton.Pressed = false;
            }

            SelectedButton = null;
            SelectedScan = null;

            SelectedButton = item;
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
                            Text = "Backpack",
                            ClipText = true
                        })
                    }
                });
            }
        }
    }
}
