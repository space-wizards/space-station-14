using Content.Server.Roles;
using Content.Shared.Chat;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;

namespace Content.Server.GameTicking.Rules;

public sealed partial class SuspicionRuleSystem
{
    /// <summary>
    /// This is DispatchServerAnnouncement but markdown is not escaped (why is it escaped in the first place on a server announcment???)
    /// </summary>
    private void SendAnnouncement(string message, Color? colorOverride = null)
    {
        _chatManager.ChatMessageToAll(
            ChatChannel.Server,
            message,
            message,
            EntityUid.Invalid,
            hideChat: false,
            recordReplay: true,
            colorOverride: colorOverride);
    }

    /// <summary>
    /// Finds all players with a specific role.
    /// </summary>
    private List<(EntityUid body, Entity<SuspicionRoleComponent> sus)> FindAllOfType(SuspicionRole role, bool filterDead = true)
    {
        var allMinds = new List<EntityUid>();
        if (filterDead)
        {
            allMinds = _mindSystem.GetAliveHumansExcept(EntityUid.Invalid);
        }
        else
        {
            var query = EntityQueryEnumerator<MindContainerComponent, HumanoidAppearanceComponent>();
            while (query.MoveNext(out var _, out var mc, out _))
            {
                // the player needs to have a mind and not be the excluded one
                if (mc.Mind == null)
                    continue;

                allMinds.Add(mc.Mind.Value);
            }
        }

        var result = new List<(EntityUid body, Entity<SuspicionRoleComponent>)>();
        foreach (var mind in allMinds)
        {
            if (!_roleSystem.MindHasRole<SuspicionRoleComponent>(mind, out var _, out var roleComp))
                continue;

            if (roleComp.Value.Comp.Role != role)
                continue;

            var entity = Comp<MindComponent>(mind).OwnedEntity;
            if (!entity.HasValue)
                continue;

            result.Add((entity.Value, roleComp.Value));
        }

        return result;
    }
}
