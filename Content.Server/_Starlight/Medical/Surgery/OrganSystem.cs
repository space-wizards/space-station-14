using Content.Server.Humanoid;
using Content.Shared.Damage;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Speech.Muting;
using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Starlight.Medical.Surgery.Events;
using Content.Shared.Starlight.Medical.Surgery.Steps.Parts;
using Content.Shared.VentCraw;

namespace Content.Server._Starlight.Medical.Surgery;
public sealed partial class OrganSystem : EntitySystem
{

    [Dependency] private readonly BlindableSystem _blindable = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FunctionalOrganComponent, SurgeryOrganImplantationCompleted>(OnFunctionalOrganImplanted);
        SubscribeLocalEvent<FunctionalOrganComponent, SurgeryOrganExtracted>(OnFunctionalOrganExtracted);

        SubscribeLocalEvent<OrganEyesComponent, SurgeryOrganImplantationCompleted>(OnEyeImplanted);
        SubscribeLocalEvent<OrganEyesComponent, SurgeryOrganExtracted>(OnEyeExtracted);

        SubscribeLocalEvent<OrganTongueComponent, SurgeryOrganImplantationCompleted>(OnTongueImplanted);
        SubscribeLocalEvent<OrganTongueComponent, SurgeryOrganExtracted>(OnTongueExtracted);

        SubscribeLocalEvent<AbductorOrganComponent, SurgeryOrganImplantationCompleted>(OnAbductorOrganImplanted);
        SubscribeLocalEvent<AbductorOrganComponent, SurgeryOrganExtracted>(OnAbductorOrganExtracted);

        SubscribeLocalEvent<DamageableComponent, SurgeryOrganImplantationCompleted>(OnOrganImplanted);
        SubscribeLocalEvent<DamageableComponent, SurgeryOrganExtracted>(OnOrganExtracted);

        SubscribeLocalEvent<OrganVisualizationComponent, SurgeryOrganImplantationCompleted>(OnVisualizationImplanted);
        SubscribeLocalEvent<OrganVisualizationComponent, SurgeryOrganExtracted>(OnVisualizationExtracted);
    }

    //

    private void OnFunctionalOrganImplanted(Entity<FunctionalOrganComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        foreach (var comp in (ent.Comp.Components ?? []).Values)
            if (!EntityManager.HasComponent(args.Body, comp.Component.GetType()))
                EntityManager.AddComponent(args.Body, comp.Component);
    }

    private void OnFunctionalOrganExtracted(Entity<FunctionalOrganComponent> ent, ref SurgeryOrganExtracted args)
    {
        foreach (var comp in (ent.Comp.Components ?? []).Values)
            if (EntityManager.HasComponent(args.Body, comp.Component.GetType()))
                EntityManager.RemoveComponent(args.Body, _compFactory.GetComponent(comp.Component.GetType()));
    }

    //

    private void OnOrganImplanted(Entity<DamageableComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        if (!TryComp<DamageableComponent>(args.Body, out var bodyDamageable)) return;

        var change = _damageableSystem.TryChangeDamage(args.Body, ent.Comp.Damage, true, false, bodyDamageable);
        if (change is not null)
            _damageableSystem.TryChangeDamage(ent.Owner, change.Invert(), true, false, ent.Comp);
    }
    private void OnOrganExtracted(Entity<DamageableComponent> ent, ref SurgeryOrganExtracted args)
    {
        if (!TryComp<OrganDamageComponent>(ent.Owner, out var damageRule)
         || damageRule.Damage is null
         || !TryComp<DamageableComponent>(args.Body, out var bodyDamageable)) return;

        var change = _damageableSystem.TryChangeDamage(args.Body, damageRule.Damage.Invert(), true, false, bodyDamageable);
        if (change is not null)
            _damageableSystem.TryChangeDamage(ent.Owner, change.Invert(), true, false, ent.Comp);
    }

    //

    private void OnAbductorOrganImplanted(Entity<AbductorOrganComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        if (TryComp<AbductorVictimComponent>(args.Body, out var victim))
            victim.Organ = ent.Comp.Organ;
        if (ent.Comp.Organ == AbductorOrganType.Vent)
            AddComp<VentCrawlerComponent>(args.Body);
    }
    private void OnAbductorOrganExtracted(Entity<AbductorOrganComponent> ent, ref SurgeryOrganExtracted args)
    {
        if (TryComp<AbductorVictimComponent>(args.Body, out var victim))
            if (victim.Organ == ent.Comp.Organ)
                victim.Organ = AbductorOrganType.None;

        if (ent.Comp.Organ == AbductorOrganType.Vent)
            RemComp<VentCrawlerComponent>(args.Body);
    }

    //

    private void OnTongueImplanted(Entity<OrganTongueComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        if (HasComp<AbductorComponent>(args.Body) || !ent.Comp.IsMuted) return;
        RemComp<MutedComponent>(args.Body);
    }

    private void OnTongueExtracted(Entity<OrganTongueComponent> ent, ref SurgeryOrganExtracted args)
    {
        ent.Comp.IsMuted = HasComp<MutedComponent>(args.Body);
        AddComp<MutedComponent>(args.Body);
    }

    //

    private void OnEyeExtracted(Entity<OrganEyesComponent> ent, ref SurgeryOrganExtracted args)
    {
        if (!TryComp<BlindableComponent>(args.Body, out var blindable)) return;

        ent.Comp.EyeDamage = blindable.EyeDamage;
        ent.Comp.MinDamage = blindable.MinDamage;
        _blindable.UpdateIsBlind((args.Body, blindable));
    }
    private void OnEyeImplanted(Entity<OrganEyesComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        if (!TryComp<BlindableComponent>(args.Body, out var blindable)) return;

        _blindable.SetMinDamage((args.Body, blindable), ent.Comp.MinDamage ?? 0);
        _blindable.AdjustEyeDamage((args.Body, blindable), (ent.Comp.EyeDamage ?? 0) - blindable.MaxDamage);
    }

    //

    private void OnVisualizationExtracted(Entity<OrganVisualizationComponent> ent, ref SurgeryOrganExtracted args) 
        => _humanoidAppearanceSystem.SetLayersVisibility(args.Body, [ent.Comp.Layer], false);
    private void OnVisualizationImplanted(Entity<OrganVisualizationComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        _humanoidAppearanceSystem.SetLayersVisibility(args.Body, [ent.Comp.Layer], true);
        _humanoidAppearanceSystem.SetBaseLayerId(args.Body, ent.Comp.Layer, ent.Comp.Prototype);
    }
}