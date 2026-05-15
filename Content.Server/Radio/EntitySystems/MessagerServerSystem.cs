using Content.Shared.Radio.EntitySystems;
using Content.Shared.Radio.Components;
using Content.Shared.DeviceNetwork.Events;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class MessagerServerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MessagerServerComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
    }

    private void OnPacketReceived(EntityUid uid, MessagerServerComponent component, DeviceNetworkPacketEvent args)
    {
        // Обработка входящих пакетов
    }

    private void UpdateUserList(EntityUid uid, MessagerServerComponent component, int userId, string userName)
    {
        component.Users.Add(new MessagerUser(userId, userName));
        Dirty(uid, component);
    }

    public static class UserDataKeys
    {
    public const string Userid = "user_uid";
    public const string UserName = "user_name";
    }
}
