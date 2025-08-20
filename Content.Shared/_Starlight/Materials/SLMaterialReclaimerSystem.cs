using System.Reflection.Metadata;
using Content.Shared.Damage;
using Content.Shared.Materials;
using Content.Shared.Mobs.Systems;

namespace Content.Shared._Starlight.Materials;

public sealed class SLMaterialReclaimerSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<MaterialReclaimerComponent,RecyclerTryGibEvent>(OnTryGib);
    }

    private void OnTryGib(Entity<MaterialReclaimerComponent> ent, ref RecyclerTryGibEvent args)
    {
        args.Handled = true;
        if (_mobState.IsDead(args.Victim)) return;
        var damageSpecifier = new DamageSpecifier(ent.Comp.EmagDamage);
        _damageable.TryChangeDamage(args.Victim, damageSpecifier);
    }
}