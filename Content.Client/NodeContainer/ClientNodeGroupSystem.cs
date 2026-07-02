using Content.Shared.NodeContainer.Systems;
using Robust.Shared.Player;

namespace Content.Client.NodeContainer;

public sealed partial class ClientNodeGroupSystem : EntitySystem
{
    [Dependency] private ISharedPlayerManager _playerMan = default!;
    [Dependency] private NodeGroupSystem _nodeGroup = default!;

    public override void Initialize()
    {
        base.Initialize();
        _playerMan.PlayerStatusChanged += PlayerManOnPlayerStatusChanged;
    }

    private void PlayerManOnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        _nodeGroup.PostInitialize();
    }
}
