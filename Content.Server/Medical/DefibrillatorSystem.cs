using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Shared.Medical;
using Content.Shared.Mind;
using Robust.Shared.Player;

namespace Content.Server.Medical;

public sealed partial class DefibrillatorSystem : SharedDefibrillatorSystem
{
    [Dependency] private EuiManager _eui = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    protected override void OpenReturnToBodyEui(Entity<MindComponent> mind, ICommonSession session)
    {
        _eui.OpenEui(new ReturnToBodyEui(mind, _mind, _player), session);
    }
}
