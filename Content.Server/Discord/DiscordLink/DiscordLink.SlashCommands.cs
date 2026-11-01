using System.Linq;
using System.Threading.Tasks;
using Content.Server.Discord.DiscordLink.Commands;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using Robust.Shared.Asynchronous;
using Robust.Shared.Reflection;

namespace Content.Server.Discord.DiscordLink;

public sealed partial class DiscordLink
{
    [Dependency] private ITaskManager _taskManager = default!;
    [Dependency] private IReflectionManager _reflectionManager = default!;
    [Dependency] private IDynamicTypeFactory _typeFactory = default!;

    /// <summary>
    ///     Slash commands of the bot, by name.
    /// </summary>
    private readonly Dictionary<string, DiscordSlashCommand> _slashCommands = new();

    private void InitializeSlashCommands()
    {
        foreach (var type in _reflectionManager.GetAllChildren<DiscordSlashCommand>())
        {
            var command = _typeFactory.CreateInstance<DiscordSlashCommand>(type);
            _slashCommands.Add(command.Name, command);
        }

        _client!.Ready += RegisterSlashCommands;
        _client.InteractionCreate += OnInteractionCreateInternal;
    }

    private void ShutdownSlashCommands()
    {
        _client!.Ready -= RegisterSlashCommands;
        _client.InteractionCreate -= OnInteractionCreateInternal;

        _slashCommands.Clear();
    }

    private async ValueTask RegisterSlashCommands(ReadyEventArgs args)
    {
        if (_client == null)
            return;

        var commands = _slashCommands.Values
            .Where(command => command.ShouldRegister())
            .Select(command => new SlashCommandProperties(command.Name, command.Description)
            {
                Options = command.Options,
            });

        try
        {
            // Overwrites all commands of the bot in the guild, so commands that should no longer be registered are removed.
            await _client.Rest.BulkOverwriteGuildApplicationCommandsAsync(args.ApplicationId, _guildId, commands);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to register Discord slash commands: {e}");
        }
    }

    private async ValueTask OnInteractionCreateInternal(NetCord.Interaction interaction)
    {
        if (interaction is not SlashCommandInteraction slashCommand ||
            !_slashCommands.TryGetValue(slashCommand.Data.Name, out var command))
        {
            return;
        }

        try
        {
            // Commands interact with the game, so they are executed on the main thread.
            var execution = new TaskCompletionSource<DiscordCommandResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            _taskManager.RunOnMainThread(() =>
            {
                try
                {
                    execution.SetResult(command.Execute(slashCommand));
                }
                catch (Exception e)
                {
                    execution.SetException(e);
                }
            });

            var result = await execution.Task;

            await slashCommand.SendResponseAsync(InteractionCallback.Message(new InteractionMessageProperties()
            {
                AllowedMentions = AllowedMentionsProperties.None,
                Content = result.Response,
                Flags = result.Ephemeral ? MessageFlags.Ephemeral : null,
            }));
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error while executing Discord slash command {command.Name}: {e}");
        }
    }
}
