using Content.Server.Construction.Components;
using Content.Server.Construction;
using Content.Shared.Damage.Systems;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Systems;
using Content.Shared.Mech.Events;

namespace Content.Server.Mech.Systems;

/// <inheritdoc/>
public sealed partial class MechSystem : SharedMechSystem
{
    [Dependency] private readonly ConstructionSystem _construction = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<MechComponent, RepairMechEvent>(OnRepairMechEvent);
    }

    private void OnDamageChanged(Entity<MechComponent> ent, ref DamageChangedEvent args)
    {
        var integrity = ent.Comp.MaxIntegrity - args.Damageable.TotalDamage;
        SetIntegrity(ent, integrity);

        // Sync construction graph with mech state
        var cc = EnsureComp<ConstructionComponent>(ent.Owner);
        if (ent.Comp.Broken)
        {
            if (_construction.ChangeGraph(ent.Owner, null, "MechRepair", "start", performActions: false, cc))
                _construction.SetPathfindingTarget(ent.Owner, "repaired", cc);
        }

        UpdateUserInterface(ent.Owner);
        UpdateHealthAlert(ent);
    }

    private void OnRepairMechEvent(Entity<MechComponent> ent, ref RepairMechEvent args)
    {
        RepairMech(ent);

        // Restore prototype-declared disassembly graph after successful repair
        var cc = EnsureComp<ConstructionComponent>(ent.Owner);
        _construction.ChangeGraph(ent.Owner, null, "MechDisassemble", "start", performActions: false, cc);
    }
}
