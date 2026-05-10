using Content.Shared._FinalStand.Shop;
using Content.Shared.Mind;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Server._FinalStand.Shop;

public sealed class FSPlayerUpgradesSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        if (!_mind.TryGetMind(ev.Entity, out var mindId, out _))
            return;
        EnsureComp<FSPlayerUpgradesComponent>(mindId);
        NotifyClient(mindId);
    }

    public int GetLevel(EntityUid mindId, string upgradeId)
    {
        return TryComp<FSPlayerUpgradesComponent>(mindId, out var comp)
            ? comp.Levels.GetValueOrDefault(upgradeId, 0)
            : 0;
    }

    public bool TryPurchase(EntityUid mindId, string upgradeId, int maxLevel, out int newLevel)
    {
        newLevel = 0;
        var comp = EnsureComp<FSPlayerUpgradesComponent>(mindId);
        var current = comp.Levels.GetValueOrDefault(upgradeId, 0);
        if (current >= maxLevel)
            return false;
        newLevel = current + 1;
        comp.Levels[upgradeId] = newLevel;
        return true;
    }

    public void ApplyUpgrades(EntityUid weapon, EntityUid mindId, List<WeaponUpgradeDef> defs)
    {
        if (!TryComp<FSPlayerUpgradesComponent>(mindId, out var upgradeComp))
            return;

        foreach (var def in defs)
        {
            var level = upgradeComp.Levels.GetValueOrDefault(def.Id, 0);
            if (level == 0)
                continue;

            switch (def.Type)
            {
                case WeaponUpgradeType.FireRate:
                    if (TryComp<GunComponent>(weapon, out var gun))
                    {
#pragma warning disable RA0002
                        gun.FireRate += def.ValuePerLevel * level;
                        gun.FireRateModified = gun.FireRate;
#pragma warning restore RA0002
                        Dirty(weapon, gun);
                    }
                    break;

                case WeaponUpgradeType.AngleMax:
                    if (TryComp<GunComponent>(weapon, out var gunA))
                    {
                        var deg = Math.Max(0.0, gunA.MaxAngle.Degrees - def.ValuePerLevel * level);
#pragma warning disable RA0002
                        gunA.MaxAngle = Angle.FromDegrees(deg);
                        gunA.MaxAngleModified = Angle.FromDegrees(deg);
#pragma warning restore RA0002
                        Dirty(weapon, gunA);
                    }
                    break;

                case WeaponUpgradeType.SpawnItem:
                    if (def.SpawnProtoId.HasValue)
                    {
                        var coords = Transform(weapon).Coordinates;
                        for (var i = 0; i < def.SpawnCountPerLevel * level; i++)
                            Spawn(def.SpawnProtoId.Value, coords);
                    }
                    break;
            }
        }
    }

    public void NotifyClient(EntityUid mindId)
    {
        if (!TryComp<FSPlayerUpgradesComponent>(mindId, out var comp))
            return;
        if (!TryComp<MindComponent>(mindId, out var mind) || mind.UserId == null)
            return;
        if (!_playerManager.TryGetSessionById(mind.UserId.Value, out var session))
            return;
        RaiseNetworkEvent(new UpgradeLevelsUpdatedEvent(new Dictionary<string, int>(comp.Levels)),
            Filter.SinglePlayer(session));
    }
}
