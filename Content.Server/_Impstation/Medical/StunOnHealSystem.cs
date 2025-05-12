using Content.Server.Electrocution;
using Content.Shared._Impstation.Medical;
using Content.Shared.Damage;
using Content.Shared.Medical;

namespace Content.Server._Impstation.Medical;

public sealed class StunOnHealSystem : EntitySystem
{
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageableComponent, HealingSuccessEvent>(OnDoAfter);
    }

    private void OnDoAfter(Entity<DamageableComponent> ent, ref HealingSuccessEvent args)
    {
        if (!TryComp<StunOnHealComponent>(args.Used, out var stun))
            return;

        var duration = stun.StunDuration;
        var damage = stun.Damage;

        _electrocution.TryDoElectrocution(ent, args.Used, damage, duration, true, ignoreInsulation: true);
    }
}
