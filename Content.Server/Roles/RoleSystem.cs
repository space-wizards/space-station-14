using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Roles;

public sealed class RoleSystem : SharedRoleSystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public string? MindGetBriefing(EntityUid? mindId)
    {
        if (mindId == null)
        {
            Log.Error($"MingGetBriefing failed for mind {mindId}");
            return null;
        }

        TryComp<MindComponent>(mindId.Value, out var mindComp);

        if (mindComp is null)
        {
            Log.Error($"MingGetBriefing failed for mind {mindId}");
            return null;
        }

        var ev = new GetBriefingEvent();

        // This is on the event because while this Entity<T> is also present on every Mind Role Entity's MindRoleComp
        // getting to there from a GetBriefing event subscription can be somewhat boilerplate
        // and this needs to be looked up for the event anyway so why calculate it again later
        ev.Mind = (mindId.Value, mindComp);

        // Briefing is no longer raised on the mind entity itself
        // because all the components that briefings subscribe to should be on Mind Role Entities
        foreach(var role in mindComp.MindRoles)
        {
            RaiseLocalEvent(role, ref ev);
        }

        return ev.Briefing;
    }

    public void RoleUpdateMessage(MindComponent mind)
    {
        if (!Player.TryGetSessionById(mind.UserId, out var session))
            return;

        if (!_proto.TryIndex(mind.RoleType, out var proto))
            return;

        var roleText = Loc.GetString(proto.Name);
        var color = proto.Color;

        //TODO add audio? Would need to be optional so it does not play on role changes that already come with their own audio
        // _audio.PlayGlobal(Sound, session);

        var message = Loc.GetString("role-type-update-message", ("color", color), ("role", roleText));
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
        _chat.ChatMessageToOne(ChatChannel.Server,
            message,
            wrappedMessage,
            default,
            false,
            session.Channel);
    }
}

/// <summary>
/// Event raised on the mind to get its briefing.
/// Handlers can either replace or append to the briefing, whichever is more appropriate.
/// </summary>
[ByRefEvent]
public sealed class GetBriefingEvent
{
    /// <summary>
    /// The text that will be shown on the Character Screen
    /// </summary>
    public string? Briefing;

    /// <summary>
    /// The Mind to whose Mind Role Entities the briefing is sent to
    /// </summary>
    public Entity<MindComponent> Mind;

    public GetBriefingEvent(string? briefing = null)
    {
        Briefing = briefing;
    }

    /// <summary>
    /// If there is no briefing, sets it to the string.
    /// If there is a briefing, adds a new line to separate it from the appended string.
    /// </summary>
    public void Append(string text)
    {
        if (Briefing == null)
        {
            Briefing = text;
        }
        else
        {
            Briefing += "\n" + text;
        }
    }
}
