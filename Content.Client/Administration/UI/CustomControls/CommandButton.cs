using System.Diagnostics.CodeAnalysis;
using Content.Client.Guidebook.Richtext;
using Robust.Client.Console;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.Administration.UI.CustomControls
{
    [Virtual]
    public class CommandButton : Button, IDocumentTag
    {
        public string? Command { get; set; }
        public bool WithDialog { get; set; }

        private DialogResultEnum? _dialogResult;

        private enum DialogResultEnum : byte
        {
            Ok = 0
        }

        public CommandButton()
        {
            OnPressed += Execute;
        }

        protected virtual bool CanPress()
        {
            return string.IsNullOrEmpty(Command) ||
                   IoCManager.Resolve<IClientConGroupController>().CanCommand(Command.Split(' ')[0]);
        }

        protected override void EnteredTree()
        {
            if (!CanPress())
            {
                Visible = false;
            }
        }

        protected virtual void Execute(ButtonEventArgs obj)
        {
            if (WithDialog)
            {
                if (!OpenDialog(obj))
                {
                    return;
                }
            }

            // Default is to execute command
            if (!string.IsNullOrEmpty(Command))
                IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand(Command);
        }

        public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
        {
            if (args.Count != 2 || !args.TryGetValue("Text", out var text) || !args.TryGetValue("Command", out var command))
            {
                Logger.Error($"Invalid arguments passed to {nameof(CommandButton)}");
                control = null;
                return false;
            }

            Command = command;
            Text = Loc.GetString(text);
            control = this;
            return true;
        }

        private bool OpenDialog(ButtonEventArgs obj)
        {
            switch (_dialogResult)
            {
                case null:
                    break;
                case DialogResultEnum.Ok:
                    _dialogResult = null;
                    return true;
            }

            var dialogWindow = new DefaultWindow { Title = Text };

            var menuContainer = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center,
                Visible = true,
                Margin = new Thickness(30, 50, 30, 10)
            };

            var ok = new Button { Text = "Ok", Margin = new Thickness(10) };
            ok.OnPressed += _ =>
            {
                _dialogResult = DialogResultEnum.Ok;
                Execute(obj);
                dialogWindow.Close();
            };

            var cancel = new Button { Text = "Cancel", Margin = new Thickness(10) };
            cancel.OnPressed += _ => dialogWindow.Close();

            menuContainer.AddChild(ok);
            menuContainer.AddChild(cancel);

            dialogWindow.AddChild(menuContainer);

            dialogWindow.OpenCentered();

            return false;
        }
    }
}
