using Content.Shared.DeadSpace.Abilities.Bloodsucker;
using Content.Shared.DeadSpace.Abilities.Bloodsucker.Components;
using Content.Server.Popups;
using Content.Shared.DeadSpace.Abilities.AutoInjectReagent;

namespace Content.Server.DeadSpace.Abilities.Bloodsucker;

public sealed partial class NeededBloodForAutoInjectSystem : SharedBloodsuckerSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NeededBloodForAutoInjectComponent, AutoInjectReagentActionEvent>(OnInject);
    }

    private void OnInject(EntityUid uid, NeededBloodForAutoInjectComponent component, AutoInjectReagentActionEvent args)
    {
        if (args.Handled)
            return;

        if (!EntityManager.TryGetComponent<BloodsuckerComponent>(uid, out var bloodsuckerComponent))
            return;

        if (bloodsuckerComponent.CountReagent < component.Cost)
        {
            _popup.PopupEntity(Loc.GetString("Недостаточно питательных веществ, у вас ") + bloodsuckerComponent.CountReagent.ToString() + Loc.GetString(" а нужно: ") + component.Cost.ToString(), uid, uid);
            args.Handled = true;
            return;
        }

        SetReagentCount(uid, -component.Cost, bloodsuckerComponent);

        _popup.PopupEntity(Loc.GetString("У вас есть ") + bloodsuckerComponent.CountReagent.ToString() + Loc.GetString(" питательных веществ"), uid, uid);
    }


}
