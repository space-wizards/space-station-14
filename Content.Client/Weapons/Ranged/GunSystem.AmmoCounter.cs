using Content.Client.Weapons.Ranged.Barrels.Components;
using Content.Shared.Weapons.Ranged;
using Robust.Client.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Weapons.Ranged;

public sealed class GunSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<MagazineAutoEjectEvent>(OnMagAutoEject);
    }

    private void OnMagAutoEject(MagazineAutoEjectEvent ev)
    {
        var player = _playerManager.LocalPlayer?.ControlledEntity;

        if (!TryComp(ev.Uid, out ClientMagazineBarrelComponent? mag) ||
            !_container.TryGetContainingContainer(ev.Uid, out var container) ||
            container.Owner != player) return;

        mag.PlayAlarmAnimation();
    }
}
