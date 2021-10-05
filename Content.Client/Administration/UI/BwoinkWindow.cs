#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Client.UserInterface;
using Content.Client.Administration;
using Content.Shared;
using Robust.Client.Credits;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.Network;
using Robust.Shared.GameObjects;
using YamlDotNet.RepresentationModel;

namespace Content.Client.UserInterface
{
    /// <summary>
    /// This window connects to a BwoinkSystem channel. BwoinkSystem manages the rest.
    /// </summary>
    public sealed class BwoinkWindow : SS14Window
    {
        [Dependency] private readonly IEntitySystemManager _systemManager = default!;

        private readonly NetUserId _channelId;
        private OutputPanel _text;
        private HistoryLineEdit _lineEdit;

        public BwoinkWindow(NetUserId channelId, string title)
        {
            IoCManager.InjectDependencies(this);

            Title = title;
            _channelId = channelId;

            var rootContainer = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical
            };

            _text = new OutputPanel()
            {
                SizeFlagsVertical = SizeFlags.FillExpand
            };
            rootContainer.AddChild(_text);

            _lineEdit = new HistoryLineEdit();
            _lineEdit.OnTextEntered += Input_OnTextEntered;
            rootContainer.AddChild(_lineEdit);

            Contents.AddChild(rootContainer);

            MinSize = (650, 450);
        }

        private void Input_OnTextEntered(LineEdit.LineEditEventArgs args)
        {
            if (!string.IsNullOrWhiteSpace(args.Text))
            {
                var bwoink = _systemManager.GetEntitySystem<BwoinkSystem>();
                bwoink.Send(_channelId, args.Text);
            }

            _lineEdit.Clear();
        }

        public void ReceiveLine(string text)
        {
            var formatted = new FormattedMessage(1);
            formatted.AddText(text);
            _text.AddMessage(formatted);
        }
    }
}
