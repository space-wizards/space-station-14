using Content.Server.DeviceNetwork.Systems;
using Content.Shared.Radio.EntitySystems;
using Content.Shared.Radio.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Robust.Shared.GameObjects;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class MessagerServerSystem : EntitySystem
{
    [Dependency] private DeviceNetworkSystem _deviceNetworkSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
    }
}

public static class UserDataKeys
{
    public const string Userid = "user_id";
    public const string UserName = "user_name";
    public const string UserList = "user_list";
}
