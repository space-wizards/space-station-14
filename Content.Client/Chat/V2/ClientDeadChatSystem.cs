using System.Diagnostics.CodeAnalysis;
using Content.Client.Administration.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Administration.Managers;
using Content.Shared.CCVar;
using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Components;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Configuration;

namespace Content.Client.Chat.V2;

public sealed class ClientDeadChatSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminManager _admin = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    public bool SendMessage(EntityUid speaker, string message, [NotNullWhen(false)] out string? reason)
    {
        if (_admin.IsAdmin(speaker) && !HasComp<GhostComponent>(speaker) || !_mobState.IsDead(speaker))
        {
            reason = "If you'd like to talk to the dead, consider dying first.";

            return false;
        }

        var messageMaxLen = _configurationManager.GetCVar(CCVars.ChatMaxMessageLength);

        if (message.Length > messageMaxLen)
        {
            reason = Loc.GetString("chat-manager-max-message-length",
                ("maxMessageLength", messageMaxLen));

            return false;
        }

        RaiseNetworkEvent(new DeadChatAttemptedEvent(GetNetEntity(speaker), message));

        reason = null;

        return true;
    }
}
