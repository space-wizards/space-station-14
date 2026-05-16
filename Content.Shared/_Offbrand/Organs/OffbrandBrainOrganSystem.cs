using Content.Shared._Offbrand.Maths;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.Body;

namespace Content.Shared._Offbrand.Organs;

public sealed partial class OffbrandBrainOrganSystem : EntitySystem
{
    [Dependency] private BrainDamageThresholdsSystem _thresholds = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OffbrandBrainOrganComponent, BodyRelayedEvent<BaseVascularToneEvent>>(OnBaseVascularTone);
        SubscribeLocalEvent<OffbrandBrainOrganComponent, OrganDamageChangedEvent>(OnOrganDamageChanged);
        SubscribeLocalEvent<OffbrandBrainOrganComponent, OrganOxygenChangedEvent>(OnOrganOxygenChanged);
        SubscribeLocalEvent<OffbrandBrainOrganComponent, OrganGotInsertedEvent>(OnGotInserted);
    }

    private void OnOrganOxygenChanged(Entity<OffbrandBrainOrganComponent> ent, ref OrganOxygenChangedEvent args)
    {
        if (Comp<OrganComponent>(ent).Body is { } body)
            _thresholds.OnAfterBrainOxygenChanged(body, (ent, Comp<DamageableOrganComponent>(ent), Comp<OxygenatableOrganComponent>(ent)));
    }

    private void OnOrganDamageChanged(Entity<OffbrandBrainOrganComponent> ent, ref OrganDamageChangedEvent args)
    {
        if (Comp<OrganComponent>(ent).Body is { } body)
            _thresholds.OnAfterBrainDamageChanged(body, (ent, Comp<DamageableOrganComponent>(ent), Comp<OxygenatableOrganComponent>(ent)));
    }

    private void OnBaseVascularTone(Entity<OffbrandBrainOrganComponent> ent, ref BodyRelayedEvent<BaseVascularToneEvent> args)
    {
        var damage = Comp<DamageableOrganComponent>(ent);
        args.Args = args.Args with
        {
            Tone = ent.Comp.VascularToneCurve.Clamped(damage.Damage.Float() / damage.MaxDamage.Float()),
        };
    }

    private void OnGotInserted(Entity<OffbrandBrainOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        var damageable = Comp<DamageableOrganComponent>(ent);
        var oxygenatable = Comp<OxygenatableOrganComponent>(ent);

        _thresholds.OnAfterBrainDamageChanged(args.Target, (ent, damageable, oxygenatable));
        _thresholds.OnAfterBrainOxygenChanged(args.Target, (ent, damageable, oxygenatable));
    }
}
