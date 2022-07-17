using Content.Shared.Revenant;
using Robust.Shared.Random;
using Content.Shared.Emag.Systems;

namespace Content.Server.Revenant.EntitySystems;

public sealed partial class RevenantSystem : EntitySystem
{
    private void OnMalfunctionAction(EntityUid uid, RevenantComponent component, RevenantMalfunctionActionEvent args)
    {
        if (args.Handled)
            return;

        if (!CanUseAbility(uid, component, component.MalfuncitonUseCost, component.MalfunctionStunDuration, component.MalfunctionCorporealDuration))
            return;

        args.Handled = true;

        var lookup = _lookup.GetEntitiesInRange(uid, component.MalfunctionRadius);

        foreach (var ent in lookup)
        {
            if (_random.Prob(component.MalfunctionEffectChance))
            {
                RaiseLocalEvent(ent, new GotEmaggedEvent(ent)); //it is going to emag itself to bypass popups and weird checks
            }
        }
    }
}
