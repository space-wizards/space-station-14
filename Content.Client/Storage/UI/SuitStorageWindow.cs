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
using static Content.Shared.Storage.SharedSuitStorageComponent;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Storage.UI
{
    public sealed class SuitStorageWindow : SS14Window
    {
        public readonly Button OpenStorageButton;
        public readonly Button CloseStorageButton;

        public SuitStorageWindow()
        {
            SetSize = MinSize = (250, 300);

            //TODO change to suit storage
            Title = Loc.GetString("suit-storage-window-title");

            Contents.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Children =
                {
                    (OpenStorageButton = new Button
                    {
                        Text = Loc.GetString("suit-storage-open-button")
                    }),
                    (CloseStorageButton = new Button
                    {
                        Text = Loc.GetString("suit-storage-close-button")
                    })
                }
            });
        }

        public void Populate(SuitStorageBoundUserInterfaceState state)
        {

        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
        }

        [DebuggerDisplay("cloningbutton {" + nameof(Index) + "}")]
        private class SuitStorageButton : Control
        {
            public string Scan { get; set; } = default!;
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
