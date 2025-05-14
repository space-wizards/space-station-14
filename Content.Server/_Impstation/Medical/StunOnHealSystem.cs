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

        SubscribeLocalEvent<StunOnHealComponent, HealingSuccessEvent>(OnDoAfter);
    }

    private void OnDoAfter(Entity<StunOnHealComponent> ent, ref HealingSuccessEvent args)
    {
        var duration = ent.Comp.StunDuration;
        var damage = ent.Comp.Damage;

        _electrocution.TryDoElectrocution(args.Target, ent, damage, duration, true, ignoreInsulation: true);
    }
}
