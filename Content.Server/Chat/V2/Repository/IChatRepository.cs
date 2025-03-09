using System.Diagnostics.CodeAnalysis;
using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Repository;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Chat.V2.Repository;

public interface IChatRepository
{
    void Initialize();

    /// <summary>
    /// Adds an <see cref="IChatEvent"/> to the repo and raises it with a UID for consumption elsewhere.
    /// </summary>
    ChatMessageWrapper? Add(
        FormattedMessage messageContent,
        CommunicationChannelPrototype communicationChannel,
        ICommonSession? senderSession,
        EntityUid? senderEntity,
        ChatMessageWrapper? parent,
        HashSet<ICommonSession>? targetSessions = null,
        ChatMessageContext? context = null
    );

    /// <summary>
    /// Returns the event associated with a UID, if it exists.
    /// </summary>
    /// <param name="id">The UID of a event.</param>
    /// <returns>The event, if it exists.</returns>
    ChatMessageWrapper? GetEventFor(uint id);

    /// <summary>
    /// Deletes a message from the repository and issues a <see cref="MessageDeletedEvent"/> that says this has happened
    /// both locally and on the network.
    /// </summary>
    /// <param name="id">The ID to delete</param>
    /// <returns>If deletion did anything</returns>
    /// <remarks>Should only be used for adminning</remarks>
    bool Delete(uint id);

    /// <summary>
    /// Nukes a user's entire chat history from the repo and issues a <see cref="MessageDeletedEvent"/> saying this has
    /// happened.
    /// </summary>
    /// <param name="userName">The user ID to nuke.</param>
    /// <param name="reason">Why nuking failed, if it did.</param>
    /// <returns>If nuking did anything.</returns>
    /// <remarks>Note that this could be a <b>very large</b> event, as we send every single event ID over the wire.
    /// By necessity we can't leak the player-source of chat messages (or if they even have the same origin) because of
    /// client modders who could use that information to cheat/metagrudge/etc >:(</remarks>
    bool NukeForUsername(string userName, [NotNullWhen(false)] out string? reason);

    /// <summary>
    /// Nukes a user's entire chat history from the repo and issues a <see cref="MessageDeletedEvent"/> saying this has
    /// happened.
    /// </summary>
    /// <param name="userId">The user ID to nuke.</param>
    /// <param name="reason">Why nuking failed, if it did.</param>
    /// <returns>If nuking did anything.</returns>
    /// <remarks>Note that this could be a <b>very large</b> event, as we send every single event ID over the wire.
    /// By necessity we can't leak the player-source of chat messages (or if they even have the same origin) because of
    /// client modders who could use that information to cheat/metagrudge/etc >:(</remarks>
    bool NukeForUserId(NetUserId userId, [NotNullWhen(false)] out string? reason);

    /// <summary>
    /// Dumps held chat storage data and refreshes the repo.
    /// </summary>
    void Refresh();
}
