using Content.Client.Chat.Managers;
using Content.Shared.CollectiveMind;
using Robust.Client.Player;

namespace Content.Client.CollectiveMind;

public sealed partial class CollectiveMindSystem : SharedCollectiveMindSystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedCollectiveMindSystem _collectiveSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CollectiveMindComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CollectiveMindComponent, ComponentRemove>(OnRemove);
    }

    public bool IsCollectiveMind => CompOrNull<CollectiveMindComponent>(_playerManager.LocalPlayer?.ControlledEntity) != null;

    private void OnInit(EntityUid uid, CollectiveMindComponent component, ComponentInit args)
    {
        _chatManager.UpdatePermissions();
    }

    private void OnRemove(EntityUid uid, CollectiveMindComponent component, ComponentRemove args)
    {
        _chatManager.UpdatePermissions();
    }
}
