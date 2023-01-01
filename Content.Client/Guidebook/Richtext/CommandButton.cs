using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Console;
using System.Diagnostics.CodeAnalysis;

namespace Content.Client.Guidebook.Richtext;

public sealed class CommandButton : Button, ITag
{
    private string _command = string.Empty;

    public bool TryParseTag(List<string> args, Dictionary<string, string> param, [NotNullWhen(true)] out Control? control, out bool instant)
    {
        instant = true;

        if (args.Count != 2)
        {
            Logger.Error($"Guidebook command button requires exactly two arguments but got got {args.Count}. Args: {string.Join(", ", args)}");
            control = null;
            return false;
        }
        control = this;
        Text = Loc.GetString(args[0]);
        _command = args[1];
        OnPressed += _ => IoCManager.Resolve<IConsoleHost>().ExecuteCommand(_command);

        return true;
    }
}
