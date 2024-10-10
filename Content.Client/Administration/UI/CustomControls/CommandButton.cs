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
    public string? RequiresConfirm { get; set; }


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

    private void Confirm()
    {
        if (string.IsNullOrEmpty(Command))
            return;

        _window = new ConfirmWindow(Command);
        _window.OpenCentered();
    }

    protected virtual void Execute(ButtonEventArgs obj)
    {
        if (string.IsNullOrEmpty(Command))
            return;

        if (RequiresConfirm != null) {
            Confirm();
            return;
        }

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
}
