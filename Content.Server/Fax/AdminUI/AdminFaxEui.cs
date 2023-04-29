using Content.Server.DeviceNetwork.Components;
using Content.Server.EUI;
using Content.Server.Ghost.Components;
using Content.Shared.Eui;
using Content.Shared.Fax;
using Content.Shared.Follower;

namespace Content.Server.Fax.AdminUI;

public sealed class AdminFaxEui : BaseEui
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private readonly FaxSystem _faxSystem;
    private readonly FollowerSystem _followerSystem;

    public AdminFaxEui()
    {
        IoCManager.InjectDependencies(this);
        _faxSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<FaxSystem>();
        _followerSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<FollowerSystem>();
    }

    public override void Opened()
    {
        StateDirty();
    }

    public override AdminFaxEuiState GetNewState()
    {
        var faxes = _entityManager.EntityQueryEnumerator<FaxMachineComponent, DeviceNetworkComponent>();
        var entries = new List<AdminFaxEntry>();
        while (faxes.MoveNext(out var uid, out var fax, out var device))
        {
            entries.Add(new AdminFaxEntry(uid, fax.FaxName, device.Address));
        }
        return new AdminFaxEuiState(entries);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case AdminFaxEuiMsg.Follow followData:
            {
                if (Player.AttachedEntity == null ||
                    !_entityManager.HasComponent<GhostComponent>(Player.AttachedEntity.Value))
                    return;

                _followerSystem.StartFollowingEntity(Player.AttachedEntity.Value, followData.TargetFax);
                break;
            }
            case AdminFaxEuiMsg.Send sendData:
            {
                var printout = new FaxPrintout(sendData.Content, sendData.Title, null, sendData.StampState, new() { sendData.From });
                _faxSystem.Receive(sendData.Target, printout);
                break;
            }
        }
    }
}
