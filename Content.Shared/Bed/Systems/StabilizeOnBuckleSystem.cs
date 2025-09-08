using Content.Shared.Bed.Components;
using Content.Shared.Body.Events;
using Content.Shared.Buckle.Components;
using Content.Shared.Examine;
using Content.Shared.Mobs.Systems;

namespace Content.Shared.Bed.Systems;

public sealed class StabilizeOnBuckleSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StabilizeOnBuckleComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<StabilizeOnBuckleComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<StabilizeOnBuckleComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<StabilizedComponent, BleedModifierEvent>(OnBleedModifier);
        SubscribeLocalEvent<StabilizedComponent, BloodlossDamageEvent>(OnBloodlossDamage);
        SubscribeLocalEvent<StabilizedComponent, SuffocationDamageEvent>(OnSuffocationDamage);
    }
    private void OnStrapped(Entity<StabilizeOnBuckleComponent> bed, ref StrappedEvent args)
    {
        EnsureComp<StabilizedComponent>(args.Buckle.Owner, out var stabilizedComp);
        stabilizedComp.Efficiency = bed.Comp.Efficiency;
        stabilizedComp.ReducesBleeding = bed.Comp.ReducesBleeding;
    }
    private void OnUnstrapped(Entity<StabilizeOnBuckleComponent> bed, ref UnstrappedEvent args)
    {
        RemComp<StabilizedComponent>(args.Buckle.Owner);
    }
    private void OnBleedModifier(Entity<StabilizedComponent> ent, ref BleedModifierEvent args)
    {
        if (_mobStateSystem.IsCritical(ent.Owner))
            args.BleedReductionAmount += ent.Comp.ReducesBleeding; // This gets called every 3 seconds. 3 ReducesBleeding = 1 per Second.
    }
    private void OnBloodlossDamage(Entity<StabilizedComponent> ent, ref BloodlossDamageEvent args)
    {
        if (_mobStateSystem.IsCritical(ent.Owner))
            args.BloodlossDamageAmount *= 1 - ent.Comp.Efficiency; // 30% Efficiency leads to 70% Bloodloss damage taken.
    }
    private void OnSuffocationDamage(Entity<StabilizedComponent> ent, ref SuffocationDamageEvent args)
    {
        if (_mobStateSystem.IsCritical(ent.Owner))
            args.AsphyxationAmount *= 1 - ent.Comp.Efficiency; // 30% Efficiency leads to 70% Asphyxiation damage taken.
    }
    private void OnExamine(Entity<StabilizeOnBuckleComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(StabilizeOnBuckleComponent)))
        {
            var comp = ent.Comp;
            var value = MathF.Round((comp.Efficiency) * 100, 1);
            args.PushMarkup(Loc.GetString("stabilizing-efficiency-value", ("value", value)));
            if (comp.ReducesBleeding > 0)
                args.PushMarkup(Loc.GetString("stabilizing-bleeding"));
        }
    }
}
