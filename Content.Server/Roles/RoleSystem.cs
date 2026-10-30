using Content.Server.Chat.Managers;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Network;

namespace Content.Server.Roles;

public sealed partial class RoleSystem : SharedRoleSystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private IChatManager _chat = default!;

    public string? MindGetBriefing(EntityUid? mindId)
    {
        if (mindId == null)
        {
            Log.Error($"MindGetBriefing failed for mind {mindId}");
            return null;
        }

        TryComp<MindComponent>(mindId.Value, out var mindComp);

        if (mindComp is null)
        {
            Log.Error($"MindGetBriefing failed for mind {mindId}");
            return null;
        }

        var ev = new GetBriefingEvent();

        // This is on the event because while this Entity<T> is also present on every Mind Role Entity's MindRoleComp
        // getting to there from a GetBriefing event subscription can be somewhat boilerplate
        // and this needs to be looked up for the event anyway so why calculate it again later
        ev.Mind = (mindId.Value, mindComp);

        // Briefing is no longer raised on the mind entity itself
        // because all the components that briefings subscribe to should be on Mind Role Entities
        foreach (var role in mindComp.MindRoleContainer.ContainedEntities)
        {
            RaiseLocalEvent(role, ref ev);
        }

        return ev.Briefing;
    }

    public void RoleUpdateMessage(MindComponent mind)
    {
        if (!Player.TryGetSessionById(mind.UserId, out var session))
            return;

        if (!ProtoMan.Resolve(mind.RoleType, out var proto))
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

    protected override void UpdateCharacterWindow(NetUserId? user, MindStringRepresentation mindString)
    {
        if (Player.TryGetSessionById(user, out var session))
        {
            RaiseNetworkEvent(new MindRoleTypeChangedEvent(), session.Channel);
        }
        else
        {
            _adminLogger.Add(
                LogType.Mind,
                LogImpact.Medium,
                $"The Character Window of {mindString} potentially did not update immediately : session error");
        }
    }
}
