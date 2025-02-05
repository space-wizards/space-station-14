using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Damage;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Interaction;
using Content.Shared.Speech.Muting;
using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Starlight.Medical.Surgery.Effects.Step;
using Content.Shared.Starlight.Medical.Surgery.Events;
using Content.Shared.Starlight.Medical.Surgery.Steps.Parts;
using Content.Shared.VentCraw;
using Robust.Shared.Prototypes;

namespace Content.Server._Starlight.Medical.Surgery;
public sealed partial class SurgeryOrganSystem : EntitySystem
{

    [Dependency] private readonly BlindableSystem _blindable = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OrganEyesComponent, SurgeryOrganImplantationCompleted>(OnEyeImplanted);
        SubscribeLocalEvent<OrganTongueComponent, SurgeryOrganImplantationCompleted>(OnTongueImplanted);
        SubscribeLocalEvent<AbductorOrganComponent, SurgeryOrganImplantationCompleted>(OnAbductorOrganImplanted);
        SubscribeLocalEvent<DamageableComponent, SurgeryOrganImplantationCompleted>(OnOrganImplanted);

        SubscribeLocalEvent<OrganEyesComponent, SurgeryOrganExtractCompleted>(OnOrganExtracted);
        SubscribeLocalEvent<OrganTongueComponent, SurgeryOrganExtractCompleted>(OnTongueExtracted);
        SubscribeLocalEvent<AbductorOrganComponent, SurgeryOrganExtractCompleted>(OnAbductorOrganExtracted);
        SubscribeLocalEvent<DamageableComponent, SurgeryOrganExtractCompleted>(OnOrganExtracted);
    }

    private void OnOrganExtracted(Entity<DamageableComponent> ent, ref SurgeryOrganExtractCompleted args)
    {
        if (!TryComp<OrganDamageComponent>(ent.Owner, out var damageRule)
         || damageRule.Damage is null
         || !TryComp<DamageableComponent>(args.Body, out var bodyDamageable)) return;

        var change = _damageableSystem.TryChangeDamage(args.Body, damageRule.Damage.Invert(), true, false, bodyDamageable);
        if (change is not null)
            _damageableSystem.TryChangeDamage(ent.Owner, change.Invert(), true, false, ent.Comp);
    }

    private void OnAbductorOrganExtracted(Entity<AbductorOrganComponent> ent, ref SurgeryOrganExtractCompleted args)
    {
        if (TryComp<AbductorVictimComponent>(args.Body, out var victim))
            if (victim.Organ == ent.Comp.Organ)
                victim.Organ = AbductorOrganType.None;

        if (ent.Comp.Organ == AbductorOrganType.Vent)
            RemComp<VentCrawlerComponent>(args.Body);
    }

    private void OnTongueExtracted(Entity<OrganTongueComponent> ent, ref SurgeryOrganExtractCompleted args)
    {
        ent.Comp.IsMuted = HasComp<MutedComponent>(args.Body);
        AddComp<MutedComponent>(args.Body);
    }

    private void OnOrganExtracted(Entity<OrganEyesComponent> ent, ref SurgeryOrganExtractCompleted args)
    {
        if (!TryComp<BlindableComponent>(args.Body, out var blindable)) return;

        ent.Comp.EyeDamage = blindable.EyeDamage;
        ent.Comp.MinDamage = blindable.MinDamage;
        _blindable.UpdateIsBlind((args.Body, blindable));
    }

    private void OnOrganImplanted(Entity<DamageableComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        if (!TryComp<DamageableComponent>(args.Body, out var bodyDamageable)) return;

        var change = _damageableSystem.TryChangeDamage(args.Body, ent.Comp.Damage, true, false, bodyDamageable);
        if (change is not null)
            _damageableSystem.TryChangeDamage(ent.Owner, change.Invert(), true, false, ent.Comp);
    }
    private void OnAbductorOrganImplanted(Entity<AbductorOrganComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        if (TryComp<AbductorVictimComponent>(args.Body, out var victim))
            victim.Organ = ent.Comp.Organ;
        if (ent.Comp.Organ == AbductorOrganType.Vent)
            AddComp<VentCrawlerComponent>(args.Body);
    }

    private void OnTongueImplanted(Entity<OrganTongueComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        if (HasComp<AbductorComponent>(args.Body) || !ent.Comp.IsMuted) return;
        RemComp<MutedComponent>(args.Body);
    }

    private void OnEyeImplanted(Entity<OrganEyesComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        if (!TryComp<BlindableComponent>(args.Body, out var blindable)) return;

        _blindable.SetMinDamage((args.Body, blindable), ent.Comp.MinDamage ?? 0);
        _blindable.AdjustEyeDamage((args.Body, blindable), (ent.Comp.EyeDamage ?? 0) - blindable.MaxDamage);
    }
}