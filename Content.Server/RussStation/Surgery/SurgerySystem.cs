using System.Linq;
using Content.Shared.Alert;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.RussStation.Surgery;
using Content.Shared.RussStation.Surgery.Components;
using Content.Shared.RussStation.Surgery.Effects;
using Content.Shared.RussStation.Surgery.Systems;
using Content.Shared.Standing;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.RussStation.Surgery;

public sealed class SurgerySystem : SharedSurgerySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, AfterInteractUsingEvent>(OnAfterInteract);
        SubscribeLocalEvent<ActiveSurgeryComponent, SurgeryStepDoAfterEvent>(OnStepDoAfter);
        SubscribeLocalEvent<ActiveSurgeryComponent, SurgeryCauteryDoAfterEvent>(OnCauteryDoAfter);
        SubscribeLocalEvent<SurgeryDrapedComponent, ComponentStartup>(OnDrapedStartup);
        SubscribeLocalEvent<SurgeryDrapedComponent, RemoveSurgeryDrapeAlertEvent>(OnRemoveDrapeAlert);

        SubscribeNetworkEvent<SelectSurgeryProcedureEvent>(OnProcedureSelected);
        SubscribeNetworkEvent<SelectOrganEvent>(OnOrganSelected);
    }

    private void OnAfterInteract(Entity<BodyComponent> target, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        var used = args.Used;
        var user = args.User;

        // No self-surgery
        if (user == target.Owner)
            return;

        // Bedsheet on non-draped patient -> open procedure selection
        if (_tags.HasTag(used, "Bedsheet") && !HasComp<SurgeryDrapedComponent>(target))
        {
            if (!_standing.IsDown(target.Owner))
            {
                _popup.PopupEntity(Loc.GetString("surgery-patient-not-down"), target, user);
                args.Handled = true;
                return;
            }

            // Don't drape yet; wait for procedure confirmation
            OpenProcedureMenu(user, target, used);
            args.Handled = true;
            return;
        }

        // Organ used on draped patient with active surgery -> insert directly
        if (HasComp<OrganComponent>(used) && HasComp<SurgeryDrapedComponent>(target) &&
            HasComp<ActiveSurgeryComponent>(target))
        {
            TryInsertOrgan(user, target, used);
            args.Handled = true;
            return;
        }

        // Must be a surgical tool on a draped patient from here
        if (!_tags.HasTag(used, "SurgeryTool") || !HasComp<SurgeryDrapedComponent>(target))
            return;

        // Cautery universal close on active surgery
        if (IsCauteryTool(used) && HasComp<ActiveSurgeryComponent>(target))
        {
            StartCauteryClose(user, target, used);
            args.Handled = true;
            return;
        }

        // Active surgery: advance step
        if (TryComp<ActiveSurgeryComponent>(target, out var active) && active.ProcedureId != null)
        {
            TryAdvanceStep(user, target, used, active);
            args.Handled = true;
            return;
        }
    }

    private void OnDrapedStartup(Entity<SurgeryDrapedComponent> ent, ref ComponentStartup args)
    {
        _alerts.ShowAlert(ent.Owner, "SurgeryDraped");
    }

    private void OnRemoveDrapeAlert(Entity<SurgeryDrapedComponent> ent, ref RemoveSurgeryDrapeAlertEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        RemComp<ActiveSurgeryComponent>(ent);
        RemComp<SurgeryDrapedComponent>(ent); // Triggers OnDrapedShutdown -> drops bedsheet, clears alert
    }

    private void OpenProcedureMenu(EntityUid surgeon, EntityUid patient, EntityUid bedsheet)
    {
        var procedures = new List<string>();
        foreach (var proto in ProtoManager.EnumeratePrototypes<SurgeryProcedurePrototype>())
        {
            procedures.Add(proto.ID);
        }

        if (procedures.Count == 0)
            return;

        if (!TryComp<ActorComponent>(surgeon, out var actor))
            return;

        RaiseNetworkEvent(new OpenSurgeryMenuEvent(GetNetEntity(patient), GetNetEntity(bedsheet), procedures), actor.PlayerSession);
    }

    private void OnProcedureSelected(SelectSurgeryProcedureEvent ev, EntitySessionEventArgs args)
    {
        if (!TryGetEntity(ev.Target, out var target))
            return;

        if (!TryGetEntity(ev.Bedsheet, out var bedsheet))
            return;

        if (!ProtoManager.TryIndex<SurgeryProcedurePrototype>(ev.ProcedureId, out var proto))
            return;

        // Validate: patient must still be down and not already draped
        if (HasComp<SurgeryDrapedComponent>(target.Value))
            return;

        if (!_standing.IsDown(target.Value))
            return;

        // Validate: bedsheet must still exist and have the Bedsheet tag
        if (!Exists(bedsheet.Value) || !_tags.HasTag(bedsheet.Value, "Bedsheet"))
            return;

        // Now drape the patient and take the bedsheet
        var draped = EnsureComp<SurgeryDrapedComponent>(target.Value);
        draped.Bedsheet = bedsheet.Value;
        Dirty(target.Value, draped);

        var drapeContainer = _container.EnsureContainer<Container>(target.Value, "surgery_drape");
        _container.Insert(bedsheet.Value, drapeContainer);

        _popup.PopupEntity(Loc.GetString("surgery-drape-patient", ("target", target.Value)), target.Value);

        var active = EnsureComp<ActiveSurgeryComponent>(target.Value);
        active.ProcedureId = ev.ProcedureId;
        active.CurrentStep = 0;
        active.Surgeon = args.SenderSession.AttachedEntity;
        Dirty(target.Value, active);

        _popup.PopupEntity(
            Loc.GetString("surgery-procedure-started", ("procedure", Loc.GetString(proto.Name)), ("target", target.Value)),
            target.Value);
    }

    private void TryAdvanceStep(EntityUid surgeon, EntityUid patient, EntityUid tool, ActiveSurgeryComponent active)
    {
        if (active.ProcedureId == null ||
            !ProtoManager.TryIndex<SurgeryProcedurePrototype>(active.ProcedureId.Value, out var proto))
            return;

        if (active.CurrentStep >= proto.Steps.Count)
            return;

        var currentStep = proto.Steps[active.CurrentStep];

        // Tool matches current step
        if (ToolMatchesStep(tool, currentStep))
        {
            StartStepDoAfter(surgeon, patient, tool, currentStep);
            return;
        }

        // Advance past repeatable step if tool matches next step
        if (currentStep.Repeatable && active.CurrentStep + 1 < proto.Steps.Count)
        {
            var nextStep = proto.Steps[active.CurrentStep + 1];
            if (ToolMatchesStep(tool, nextStep))
            {
                active.CurrentStep++;
                Dirty(patient, active);
                StartStepDoAfter(surgeon, patient, tool, nextStep);
                return;
            }
        }

        _popup.PopupEntity(Loc.GetString("surgery-wrong-tool"), patient, surgeon);
    }

    private void StartStepDoAfter(EntityUid surgeon, EntityUid patient, EntityUid tool, SurgeryStep step)
    {
        var duration = GetStepDuration(step, patient);

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            surgeon,
            duration,
            new SurgeryStepDoAfterEvent(),
            patient,
            target: patient,
            used: tool)
        {
            NeedHand = true,
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void StartCauteryClose(EntityUid surgeon, EntityUid patient, EntityUid tool)
    {
        var duration = TimeSpan.FromSeconds(2f * GetSurfaceSpeedModifier(patient));

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            surgeon,
            duration,
            new SurgeryCauteryDoAfterEvent(),
            patient,
            target: patient,
            used: tool)
        {
            NeedHand = true,
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnStepDoAfter(Entity<ActiveSurgeryComponent> ent, ref SurgeryStepDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        var patient = ent.Owner;
        var active = ent.Comp;

        if (active.ProcedureId == null ||
            !ProtoManager.TryIndex<SurgeryProcedurePrototype>(active.ProcedureId.Value, out var proto))
            return;

        if (active.CurrentStep >= proto.Steps.Count)
            return;

        var step = proto.Steps[active.CurrentStep];

        // Apply side effects
        ApplyStepEffects(patient, step);

        // Popup
        if (!string.IsNullOrEmpty(step.Popup) && args.User is { } user)
            _popup.PopupEntity(Loc.GetString(step.Popup, ("user", user), ("target", patient)), patient);

        // Trigger effect if this step has one
        if (step.Effect != null)
            HandleEffect(args.User, patient, step.Effect);

        // Advance step (unless repeatable)
        if (!step.Repeatable)
        {
            active.CurrentStep++;
            Dirty(patient, active);
        }
        else if (step.Effect != null)
        {
            // Effect-based repeatable steps (e.g. organ manipulation) don't auto-repeat;
            // the surgeon manually uses the tool or organ again.
        }
        else
        {
            // Auto-repeat if the step can still heal something
            args.Repeat = StepCanStillHeal(patient, step);

            if (!args.Repeat)
                _popup.PopupEntity(Loc.GetString("surgery-step-repeat-done"), patient);
        }

        // Procedure steps exhausted, wait for cautery to close
        if (active.CurrentStep >= proto.Steps.Count)
            _popup.PopupEntity(Loc.GetString("surgery-procedure-complete"), patient);
    }

    private void OnCauteryDoAfter(Entity<ActiveSurgeryComponent> ent, ref SurgeryCauteryDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        ApplyCauteryClose(ent.Owner, args.User);
    }

    private void ApplyStepEffects(EntityUid patient, SurgeryStep step)
    {
        if (step.Damage != null)
            _damageable.TryChangeDamage(patient, step.Damage);

        if (step.Healing != null)
        {
            if (step.HealingTotal > 0 && TryComp<DamageableComponent>(patient, out var damageable))
            {
                // Distribute a fixed healing budget proportionally across actual damage
                var budget = FixedPoint2.New(step.HealingTotal);
                var currentDamage = _damageable.GetPositiveDamage((patient, damageable));
                var totalDamage = FixedPoint2.Zero;

                foreach (var type in step.Healing.DamageDict.Keys)
                {
                    if (currentDamage.DamageDict.TryGetValue(type, out var current))
                        totalDamage += current;
                }

                if (totalDamage > 0)
                {
                    var healSpec = new DamageSpecifier();
                    foreach (var type in step.Healing.DamageDict.Keys)
                    {
                        if (currentDamage.DamageDict.TryGetValue(type, out var current) && current > 0)
                        {
                            var share = budget * current / totalDamage;
                            healSpec.DamageDict[type] = -share;
                        }
                    }

                    _damageable.TryChangeDamage(patient, healSpec, true);
                }
            }
            else
            {
                // No budget cap: heal each type independently
                var negated = new DamageSpecifier(step.Healing);
                foreach (var key in negated.DamageDict.Keys.ToList())
                {
                    negated.DamageDict[key] = -negated.DamageDict[key];
                }

                _damageable.TryChangeDamage(patient, negated, true);
            }
        }

        if (step.BleedModifier != 0)
            _bloodstream.TryModifyBleedAmount((patient, null), step.BleedModifier);
    }

    private void ApplyCauteryClose(EntityUid patient, EntityUid? surgeon)
    {
        // Cautery burn damage
        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Heat", FixedPoint2.New(2));
        _damageable.TryChangeDamage(patient, damage);

        // Stop all bleeding
        _bloodstream.TryModifyBleedAmount((patient, null), -100f);

        if (surgeon != null)
            _popup.PopupEntity(Loc.GetString("surgery-step-cauterize", ("user", surgeon.Value), ("target", patient)), patient);

        // Clean up
        RemComp<ActiveSurgeryComponent>(patient);
        RemComp<SurgeryDrapedComponent>(patient); // Triggers OnDrapedShutdown -> drops bedsheet
    }

    private void TryInsertOrgan(EntityUid surgeon, EntityUid patient, EntityUid organ)
    {
        if (!TryComp<OrganComponent>(organ, out var organComp))
            return;

        if (!TryComp<BodyComponent>(patient, out var body) || body.Organs == null)
            return;

        // Block if the patient already has an organ of the same category
        if (organComp.Category != null)
        {
            foreach (var existing in body.Organs.ContainedEntities)
            {
                if (TryComp<OrganComponent>(existing, out var existingOrgan) &&
                    existingOrgan.Category == organComp.Category)
                {
                    _popup.PopupEntity(
                        Loc.GetString("surgery-organ-already-exists", ("organ", MetaData(organ).EntityName)),
                        patient, surgeon);
                    return;
                }
            }
        }

        _container.Insert(organ, body.Organs, force: true);
        _popup.PopupEntity(
            Loc.GetString("surgery-organ-inserted", ("organ", MetaData(organ).EntityName)),
            patient);
    }

    private void HandleEffect(EntityUid? surgeon, EntityUid patient, ISurgeryEffect effect)
    {
        switch (effect)
        {
            case HealDamageEffect heal:
                if (heal.Healing != null)
                {
                    var negated = new DamageSpecifier(heal.Healing);
                    foreach (var key in negated.DamageDict.Keys.ToList())
                    {
                        negated.DamageDict[key] = -negated.DamageDict[key];
                    }

                    _damageable.TryChangeDamage(patient, negated, true);
                }

                break;

            case RemoveOrganEffect:
                if (surgeon == null || !TryComp<ActorComponent>(surgeon, out var actor))
                    return;

                if (!TryComp<BodyComponent>(patient, out var body) || body.Organs == null)
                    return;

                var organs = new List<(NetEntity, string, string?)>();
                foreach (var organ in body.Organs.ContainedEntities)
                {
                    // Skip limbs, only show internal organs
                    if (!TryComp<OrganComponent>(organ, out var organComp))
                        continue;

                    if (IsLimbCategory(organComp.Category))
                        continue;

                    var meta = MetaData(organ);
                    organs.Add((GetNetEntity(organ), meta.EntityName, meta.EntityPrototype?.ID));
                }

                if (organs.Count > 0)
                    RaiseNetworkEvent(new OpenOrganMenuEvent(GetNetEntity(patient), organs), actor.PlayerSession);

                break;

            // InsertOrganEffect is no longer used via HandleEffect;
            // organ insertion is handled directly via TryInsertOrgan in OnAfterInteract.
        }
    }

    private bool StepCanStillHeal(EntityUid patient, SurgeryStep step)
    {
        if (step.Healing == null || step.HealingTotal <= 0)
            return false;

        if (!TryComp<DamageableComponent>(patient, out var damageable))
            return false;

        var currentDamage = _damageable.GetPositiveDamage((patient, damageable));

        foreach (var type in step.Healing.DamageDict.Keys)
        {
            if (currentDamage.DamageDict.TryGetValue(type, out var amount) && amount > 0)
                return true;
        }

        return false;
    }

    private static bool IsLimbCategory(ProtoId<OrganCategoryPrototype>? category)
    {
        if (category == null)
            return false;

        return category.Value.Id is
            "Torso" or "Head" or
            "ArmLeft" or "ArmRight" or
            "HandLeft" or "HandRight" or
            "LegLeft" or "LegRight" or
            "FootLeft" or "FootRight";
    }

    private void OnOrganSelected(SelectOrganEvent ev, EntitySessionEventArgs args)
    {
        if (!TryGetEntity(ev.Target, out var patient) || !TryGetEntity(ev.OrganId, out var organ))
            return;

        if (!TryComp<BodyComponent>(patient.Value, out var body) || body.Organs == null)
            return;

        if (body.Organs == null || !body.Organs.ContainedEntities.Contains(organ.Value))
            return;

        _container.Remove(organ.Value, body.Organs);
        _xform.DropNextTo(organ.Value, patient.Value);

        _popup.PopupEntity(
            Loc.GetString("surgery-organ-removed", ("organ", MetaData(organ.Value).EntityName)),
            patient.Value);
    }
}
