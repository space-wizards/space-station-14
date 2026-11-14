using Content.Client.Administration.Systems;
using Content.Shared.Commands;
using Robust.Shared.Console;

namespace Content.Client.Administration;

/// <summary>
/// Enables or disables the admin "player info" overlay.
/// </summary>
/// <seealso cref="AdminNameOverlay"/>
public sealed partial class AdminOverlayCommand : LocalizedEntityCommands
{
    [Dependency] private AdminSystem _adminSystem = null!;

    public override string Command => "admin_overlay";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!CommandHelper.CheckExactlyOneArgument(Loc, shell, args))
            return;

        if (!CommandHelper.ParseArgumentBoolean(Loc, shell, args[0], out var boolean))
            return;

        if (boolean)
        {
            _adminSystem.AdminOverlayOn();
        }
        else
        {
            _adminSystem.AdminOverlayOff();
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.Booleans,
                Loc.GetString("cmd-admin_overlay-arg-state"));
        }

        return CompletionResult.Empty;
    }
}
