using System.Diagnostics.CodeAnalysis;
using Content.Client.Guidebook.Richtext;
using Robust.Client.Console;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Administration.UI.CustomControls;

[Virtual]
public class CommandButton : Button, IDocumentTag
{
    private ConfirmWindow? _window;

    public string? Command { get; set; }
    public string? ConfirmationPrompt { get; set; }

    public CommandButton()
    {
        OnPressed += TryExecute;
    }

    protected virtual bool CanPress()
    {
        return string.IsNullOrEmpty(Command) ||
            IoCManager.Resolve<IClientConGroupController>().CanCommand(Command.Split(' ')[0]);
    }

    protected override void EnteredTree()
    {
        Visible = CanPress();
    }

    private void Confirm(ButtonEventArgs obj)
    {
        if (ConfirmationPrompt is null || Command is null)
        {
            return;
        }

        if (_window is null)
        {
            _window = new ConfirmWindow(
                () => { Execute(obj); },    // confirm
                () => { },                  // cancel
                Loc.GetString("conform-command-message", ("command", Command), ("message", ConfirmationPrompt))
            );
        }

        if (_window is not null && !_window.IsOpen)
        {
            _window.OpenCentered();
        }

    }

    private void TryExecute(ButtonEventArgs obj)
    {
        if (!string.IsNullOrEmpty(ConfirmationPrompt))
        {
            Confirm(obj);
            return;
        }

        Execute(obj);
    }

    protected virtual void Execute(ButtonEventArgs obj)
    {
        if (string.IsNullOrEmpty(Command))
            return;

        IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand(Command);
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        if (!(args.Count == 3 || args.Count == 2) || !args.TryGetValue("Text", out var text) || !args.TryGetValue("Command", out var command))
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
}
