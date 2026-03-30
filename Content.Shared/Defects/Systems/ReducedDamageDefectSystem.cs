using System.Linq;
using Content.Shared.Defects.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Network;

namespace Content.Shared.Defects.Systems;

/// <summary>
/// At MapInit, scales down all damage values on MeleeWeaponComponent by
/// DamageMultiplier. Runs after DefectSystem so only surviving defects apply.
/// </summary>
public sealed class ReducedDamageDefectSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReducedDamageDefectComponent, MapInitEvent>(OnMapInit,
            after: new[] { typeof(DefectSystem) });
    }

    private void OnMapInit(Entity<ReducedDamageDefectComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryComp<MeleeWeaponComponent>(ent.Owner, out var melee))
            return;

        foreach (var key in melee.Damage.DamageDict.Keys.ToList())
        {
            melee.Damage.DamageDict[key] = FixedPoint2.New((float) melee.Damage.DamageDict[key] * ent.Comp.DamageMultiplier);
        }

        Dirty(ent.Owner, melee);
    }
}
