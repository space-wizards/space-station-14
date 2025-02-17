using Content.Shared.DeadSpace.Abilities.SpawnAbility.Components;
using Content.Shared.DeadSpace.Abilities.Bloodsucker.Components;
using Content.Shared.DeadSpace.Abilities.Bloodsucker;
using Content.Server.Popups;
using Content.Shared.DeadSpace.Abilities.SpawnAbility;

namespace Content.Server.DeadSpace.Abilities.Bloodsucker;

public sealed partial class NeededBloodForSpawnAgainstSystem : SharedBloodsuckerSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NeededBloodForSpawnAgainstComponent, SpawnAgainstDoAfterEvent>(OnDoAfterSpawnAgainst);
    }

    private void OnDoAfterSpawnAgainst(EntityUid uid, NeededBloodForSpawnAgainstComponent component, SpawnAgainstDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!EntityManager.TryGetComponent<BloodsuckerComponent>(uid, out var bloodsuckerComponent))
            return;

        if (!EntityManager.TryGetComponent<SpawnAgainstComponent>(uid, out var spawnAgainstComponent))
            return;
        
        float price = CalculateBloodCost(spawnAgainstComponent.SelectEntity, component.BloodCosts, component.DefaultCost);

        if (bloodsuckerComponent.CountReagent < price)
        {
            _popup.PopupEntity(Loc.GetString("Недостаточно питательных веществ, у вас ") + bloodsuckerComponent.CountReagent.ToString() + Loc.GetString(" а нужно: ") + price.ToString(), uid, uid);
            args.Handled = true;
            return;
        }

        SetReagentCount(uid, -price, bloodsuckerComponent);

        _popup.PopupEntity(Loc.GetString("У вас есть ") + bloodsuckerComponent.CountReagent.ToString() + Loc.GetString(" питательных веществ"), uid, uid);
    }
}
