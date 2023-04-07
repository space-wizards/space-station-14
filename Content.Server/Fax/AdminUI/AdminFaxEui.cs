using Content.Server.DeviceNetwork.Components;
using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Fax;

namespace Content.Server.Fax.AdminUI;

public sealed class AdminFaxEui : BaseEui
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private readonly FaxSystem _faxSystem;

    public AdminFaxEui()
    {
        IoCManager.InjectDependencies(this);
        _faxSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<FaxSystem>();
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
        switch (msg)
        {
            case AdminFaxEuiMsg.Close:
            {
                Close();
                break;
            }
            case AdminFaxEuiMsg.Send sendData:
            {
                var printout = new FaxPrintout(sendData.Content, sendData.Name);
                _faxSystem.Receive(sendData.TargetFax, printout);
                break;
            }
        }
    }
}
