using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Events;
using Content.Shared.Mech.Systems;

namespace Content.Server.Mech.Systems;

public sealed class MechSystem : SharedMechSystem
{
    [Dependency] private readonly ConstructionSystem _construction = default!;

    private const string MechRepairGraph = "MechRepair";
    private const string MechDisassembleGraph = "MechDisassemble";

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
        SetIntegrity(ent.AsNullable(), integrity);

        // Sync construction graph with mech state.
        var cc = EnsureComp<ConstructionComponent>(ent.Owner);
        if (ent.Comp.Broken)
        {
            if (_construction.ChangeGraph(ent.Owner, null, MechRepairGraph, "start", performActions: false, cc))
                _construction.SetPathfindingTarget(ent.Owner, "repaired", cc);
        }

        UpdateMechUi(ent.Owner);
        UpdateHealthAlert(ent.AsNullable());
    }

    private void OnRepairMechEvent(Entity<MechComponent> ent, ref RepairMechEvent args)
    {
        RepairMech(ent.AsNullable());

        // Restore prototype-declared disassembly graph after successful repair.
        var cc = EnsureComp<ConstructionComponent>(ent.Owner);
        _construction.ChangeGraph(ent.Owner, null, MechDisassembleGraph, "start", performActions: false, cc);
    }
}
