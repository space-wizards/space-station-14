// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Chat.Managers;
using Content.Shared.Mind;
using Content.Shared.Chat;
using Content.Shared.DeadSpace.Events.Roles.Components;

namespace Content.Server.DeadSpace.Events.Roles;

public sealed class AutoDeleteOnDeathSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EventRoleComponent, ComponentAdd>(OnComponentAdd);
        SubscribeLocalEvent<EventRoleComponent, ComponentRemove>(OnComponentRem);
    }

    private void OnComponentAdd(EntityUid uid, EventRoleComponent component, ComponentAdd args)
    {
        if (!_mindSystem.TryGetMind(uid, out _, out var mind))
        {
            return;
        }

        if (mind.Session != null)
        {
            var message = Loc.GetString("eventrole-giverolemassage");
            var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
            _chatManager.ChatMessageToOne(ChatChannel.Server, message, wrappedMessage, default, false, mind.Session.Channel, Color.FromHex("#5e9cff"));
        }
    }

    private void OnComponentRem(EntityUid uid, EventRoleComponent component, ComponentRemove args)
    {
        if (!_mindSystem.TryGetMind(uid, out _, out var mind))
        {
            return;
        }

        if (mind.Session != null)
        {
            var message = Loc.GetString("eventrole-takerolemassage");
            var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
            _chatManager.ChatMessageToOne(ChatChannel.Server, message, wrappedMessage, default, false, mind.Session.Channel, Color.FromHex("#5e9cff"));
        }
    }
}
