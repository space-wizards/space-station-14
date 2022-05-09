using Content.Shared.LandMines;
using Robust.Client;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Client.LandMines;

public sealed class KickMineManager
{
    private bool _fakeLossEnabled;

    [Dependency] private readonly IBaseClient _baseClient = default!;
    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgKickMineDisconnect>(RxCallback);

        _baseClient.RunLevelChanged += BaseClientOnRunLevelChanged;
    }

    private void BaseClientOnRunLevelChanged(object? sender, RunLevelChangedEventArgs e)
    {
        if (_fakeLossEnabled && e.OldLevel == ClientRunLevel.InGame)
        {
            _cfg.SetCVar(CVars.NetFakeLoss, 0);

            _fakeLossEnabled = false;
        }
    }

    private void RxCallback(MsgKickMineDisconnect message)
    {
        _fakeLossEnabled = true;

        _cfg.SetCVar(CVars.NetFakeLoss, 1);
    }
}
