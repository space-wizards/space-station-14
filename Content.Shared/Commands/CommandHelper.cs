using Robust.Shared.Console;

namespace Content.Shared.Commands;

/// <summary>
/// Helper functions for common command things.
/// </summary>
public static class CommandHelper
{
    /// <summary>
    /// Check that there is exactly 1 input argument. Returns false and prints an error if not.
    /// </summary>
    public static bool CheckExactlyOneArgument(ILocalizationManager loc, IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(loc.GetString("shell-need-exactly-one-argument"));
            return false;
        }

        return true;
    }

    /// <summary>
    /// Parse an argument as boolean. Returns false and prints an error if not.
    /// </summary>
    public static bool ParseArgumentBoolean(ILocalizationManager loc, IConsoleShell shell, string value, out bool boolean)
    {
        if (!bool.TryParse(value, out boolean))
        {
            shell.WriteError(loc.GetString("shell-invalid-bool-value", ("value", value)));
            return false;
        }

        return true;
    }
}
