using Content.Shared.GhostKick;
using Robust.Client;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Client.GhostKick;

[InjectDependencies]
public sealed partial class GhostKickManager
{
    private bool _fakeLossEnabled;

    [Dependency] private IBaseClient _baseClient = default!;
    [Dependency] private IClientNetManager _netManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgGhostKick>(RxCallback);

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

    private void RxCallback(MsgGhostKick message)
    {
        _fakeLossEnabled = true;

        _cfg.SetCVar(CVars.NetFakeLoss, 1);
    }
}
