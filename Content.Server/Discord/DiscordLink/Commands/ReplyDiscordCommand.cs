using System.Linq;
using Content.Server.Administration.Systems;
using Content.Shared.CCVar;
using NetCord;
using NetCord.Rest;
using Robust.Shared.Configuration;

namespace Content.Server.Discord.DiscordLink.Commands;

/// <summary>
/// Sends a message to the game from a Discord channel linked to it: OOC, admin chat or an ahelp thread.
/// Replaces relaying regular messages when the bot is not allowed to read them.
/// </summary>
public sealed partial class ReplyDiscordCommand : DiscordSlashCommand
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private DiscordChatLink _chatLink = default!;

    private const string MessageOption = "message";

    // Leaves space for the author in the channel message, which can be at most 2000 characters long
    private const int MessageMaxLength = 1800;

    public override string Name => "reply";

    public override string Description => "Send a message to the game from a linked channel.";

    public override IEnumerable<ApplicationCommandOptionProperties> Options =>
    [
        new ApplicationCommandOptionProperties(ApplicationCommandOptionType.String, MessageOption, "The message to send.")
        {
            Required = true,
            MaxLength = MessageMaxLength,
        },
    ];

    public override bool ShouldRegister()
    {
        // Regular messages are relayed instead
        return !_config.GetCVar(CCVars.DiscordMessageContentIntent);
    }

    public override DiscordCommandResult Execute(SlashCommandInteraction interaction)
    {
        var message = interaction.Data.Options.FirstOrDefault(option => option.Name == MessageOption)?.Value;
        var author = interaction.User.Username;
        var channelId = interaction.Channel.Id;

        if (string.IsNullOrWhiteSpace(message))
            return new DiscordCommandResult(Loc.GetString("discord-reply-command-failed"));

        //Uhh dont really like this, but i think OK for now.
        var relayed = _chatLink.TryRelayToGame(channelId, author, message)
                      || _entitySystemManager.GetEntitySystem<BwoinkSystem>().TryRelayFromDiscord(channelId, author, message);

        if (!relayed)
            return new DiscordCommandResult(Loc.GetString("discord-reply-command-failed"));

        // Shown to everyone, so others can see what was sent to the game and by whom
        return new DiscordCommandResult(
            Loc.GetString("discord-reply-command-relayed", ("author", author), ("message", message)),
            Ephemeral: false);
    }
}
