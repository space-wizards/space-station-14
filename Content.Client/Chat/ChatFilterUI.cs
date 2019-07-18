using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

using Content.Client.Chat;

namespace Content.Client.Chat
{
    public class ChatFilterUI : SS14Window
    {
        protected override void Initialize()
        {
            base.Initialize();

            Title = "Filter Channels";

            var margin = new MarginContainer()
            {
                MarginTop = 5f,
                MarginLeft = 5f,
                MarginRight = -5f,
                MarginBottom = -5f,
            };

            margin.SetAnchorAndMarginPreset(LayoutPreset.TopRight);

            var vbox = new VBoxContainer();

            vbox.SetAnchorAndMarginPreset(LayoutPreset.TopRight);

            var descMargin = new MarginContainer()
            {
                MarginTop = 5f,
                MarginLeft = 5f,
                MarginRight = -5f,
                MarginBottom = -5f,
                SizeFlagsHorizontal = SizeFlags.Fill,
                SizeFlagsStretchRatio = 2,
            };

            var hbox = new HBoxContainer()
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
            };

            var vboxInfo = new VBoxContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 3,
            };


        }
    }
}
