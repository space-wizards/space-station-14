using Content.Server.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Heretic.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.Ritual;

public sealed partial class RitualAshAscendBehavior : RitualSacrificeBehavior
{
    private List<EntityUid> burningUids = new();

    // check for burning corpses
    public override bool Execute(RitualData args, out string? outstr)
    {
        if (!base.Execute(args, out outstr))
            return false;

        for (int i = 0; i < Max; i++)
        {
            if (args.EntityManager.TryGetComponent<FlammableComponent>(uids[i], out var flam))
                if (flam.OnFire)
                    burningUids.Add(uids[i]);
        }

        if (burningUids.Count < Min)
        {
            outstr = Loc.GetString("heretic-ritual-fail-sacrifice-ash");
            return false;
        }

        outstr = null;
        return true;
    }

    public override void Finalize(RitualData args)
    {
        for (int i = 0; i < Max; i++)
        {
            // YES!!! ASH!!!
            if (args.EntityManager.TryGetComponent<DamageableComponent>(uids[i], out var dmg))
            {
                var prot = (ProtoId<DamageGroupPrototype>) "Burn";
                var dmgtype = _proto.Index(prot);
                _damage.TryChangeDamage(uids[i], new DamageSpecifier(dmgtype, 3984f), true);
            }
        }

        // reset it because blehhh
        uids = new();
    }
}
