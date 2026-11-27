using NetCord;
using NetCord.Rest;

namespace Content.Server.Discord.DiscordLink.Commands;

/// <summary>
/// A slash command of the Discord bot.
/// All non-abstract children are found, instantiated and registered in the Discord guild by <see cref="DiscordLink"/>.
/// Dependencies are injected.
/// </summary>
public abstract class DiscordSlashCommand
{
    /// <summary>
    /// The name of the command, as typed after the slash.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// The description of the command shown in Discord.
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// The options (arguments) of the command.
    /// </summary>
    public virtual IEnumerable<ApplicationCommandOptionProperties> Options => [];

    /// <summary>
    /// Whether the command should be registered in the Discord guild with the current configuration.
    /// </summary>
    public virtual bool ShouldRegister()
    {
        return true;
    }

    /// <summary>
    /// Executes the command. This is called on the main thread.
    /// </summary>
    public abstract DiscordCommandResult Execute(SlashCommandInteraction interaction);
}

/// <summary>
/// The result of executing a <see cref="DiscordSlashCommand"/>.
/// </summary>
/// <param name="Response">The response to the command. Sent without any mentions.</param>
/// <param name="Ephemeral">Whether the response is shown only to the user who used the command.</param>
public readonly record struct DiscordCommandResult(string Response, bool Ephemeral = true);
