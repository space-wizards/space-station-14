using System.Diagnostics.CodeAnalysis;
using Content.Client.Guidebook.Richtext;
using Robust.Client.Console;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Administration.UI.CustomControls
{
    [Virtual]
    public class CommandButton : Button, IDocumentTag
    {
        [Dependency] private readonly ILogManager _logManager = default!;
        public string? Command { get; set; }
        private ISawmill _sawmill = default!;

        public CommandButton()
        {
            OnPressed += Execute;
            _sawmill = _logManager.GetSawmill("commandbutton");
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
            // Default is to execute command
            if (!string.IsNullOrEmpty(Command))
                IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand(Command);
        }

        public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
        {
            if (args.Count != 2 || !args.TryGetValue("Text", out var text) || !args.TryGetValue("Command", out var command))
            {
                _sawmill.Error($"Invalid arguments passed to {nameof(CommandButton)}");
                control = null;
                return false;
            }

            Command = command;
            Text = Loc.GetString(text);
            control = this;
            return true;
        }
    }
}
