using Content.Shared.DeadSpace.Abilities.Bloodsucker;
using Content.Shared.DeadSpace.Abilities.Bloodsucker.Components;
using Content.Server.Popups;
using Content.Shared.DeadSpace.Abilities.Evolution.Components;
using Content.Shared.DeadSpace.Abilities.Evolution;

namespace Content.Server.DeadSpace.Abilities.Bloodsucker;

public sealed partial class NeededBloodForEvolutionSystem : SharedBloodsuckerSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NeededBloodForEvolutionComponent, EvolutionDoAfterEvent>(OnDoAfterEvolution);
    }

    private void OnDoAfterEvolution(EntityUid uid, NeededBloodForEvolutionComponent component, EvolutionDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!EntityManager.TryGetComponent<BloodsuckerComponent>(uid, out var bloodsuckerComponent))
            return;

        if (!EntityManager.TryGetComponent<EvolutionComponent>(uid, out var evolutionComponent))
            return;

        float price = CalculateBloodCost(evolutionComponent.SelectEntity, component.BloodCosts, component.DefaultCost);

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
