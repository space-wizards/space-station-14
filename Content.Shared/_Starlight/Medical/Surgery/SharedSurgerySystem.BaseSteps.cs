using Content.Shared.Body.Part;
using Content.Shared.Buckle.Components;
using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Starlight.Medical.Surgery.Effects.Step;
using Content.Shared.Starlight.Medical.Surgery.Events;
using Content.Shared.Starlight.Medical.Surgery.Steps;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Shared.Starlight.Medical.Surgery;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
public abstract partial class SharedSurgerySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    private void InitializeSteps()
    {
        SubscribeLocalEvent<SurgeryStepComponent, SurgeryStepCompleteEvent>(OnStepComplete);
        SubscribeLocalEvent<SurgeryClearProgressComponent, SurgeryStepCompleteEvent>(OnClearProgressStep);
        SubscribeLocalEvent<SurgeryStepComponent, SurgeryStepEvent>(OnStep);
        SubscribeLocalEvent<SurgeryTargetComponent, SurgeryDoAfterEvent>(OnTargetDoAfter);

        SubscribeLocalEvent<SurgeryStepComponent, SurgeryCanPerformStepEvent>(OnCanPerformStep);

        Subs.BuiEvents<SurgeryTargetComponent>(SurgeryUIKey.Key, subs => subs.Event<SurgeryStepChosenBuiMsg>(OnSurgeryTargetStepChosen));
    }
    private void OnTargetDoAfter(Entity<SurgeryTargetComponent> ent, ref SurgeryDoAfterEvent args)
    {
        if (args.Cancelled ||
            args.Handled ||
            args.Target is not { } target ||
            !IsSurgeryValid(ent, target, args.Surgery, args.Step, out var surgery, out var part, out var step) ||
            !PreviousStepsComplete(ent, part, surgery, args.Step) ||
            !CanPerformStep(args.User, ent, part.Comp.PartType, step, false))
        {
            Log.Warning($"{ToPrettyString(args.User)} tried to start invalid surgery.");
            Dirty(ent);
            if (args.Target.HasValue && TryComp<BodyPartComponent>(args.Target.Value, out var dirtyPart))
                Dirty(args.Target.Value, dirtyPart, Comp<MetaDataComponent>(args.Target.Value));
            return;
        }

        if (!_random.Prob(args.SuccessRate))
        {
            if (_net.IsClient) return;
            _popup.PopupEntity("Because of a careless tool, your hand shook. You need to start this step all over again!", args.User, PopupType.SmallCaution);
            return;
        }

        var ev = new SurgeryStepEvent(args.User, ent, part, GetTools(args.User))
        {
            StepProto = args.Step,
            SurgeryProto = args.Surgery,
        };
        RaiseLocalEvent(step, ref ev);

        if (ev.IsCancelled) return;
        var evComplete = new SurgeryStepCompleteEvent(args.User, ent, part, GetTools(args.User))
        {
            StepProto = args.Step,
            SurgeryProto = args.Surgery,
            IsFinal = surgery.Comp.Steps[^1] == args.Step,
        };
        RaiseLocalEvent(step, ref evComplete);

        RefreshUI(ent);
    }

    private void OnClearProgressStep(Entity<SurgeryClearProgressComponent> ent, ref SurgeryStepCompleteEvent args)
    {
        var progress = Comp<SurgeryProgressComponent>(args.Part);
        progress.CompletedSteps.Clear();
        progress.CompletedSurgeries.Clear();
    }
    private void OnStepComplete(Entity<SurgeryStepComponent> ent, ref SurgeryStepCompleteEvent args)
    {
        if (TryComp<SurgeryClearProgressComponent>(ent, out _)) return;
        if (TryComp<SurgeryProgressComponent>(args.Part, out var progress))
        {
            progress.CompletedSteps.Add($"{args.SurgeryProto}:{args.StepProto}");
            if (!progress.StartedSurgeries.Contains(args.SurgeryProto) && !args.IsFinal)
                progress.StartedSurgeries.Add(args.SurgeryProto);
            if (progress.StartedSurgeries.Contains(args.SurgeryProto) && args.IsFinal)
                progress.StartedSurgeries.Remove(args.SurgeryProto);
        }
        else
        {
            progress = new SurgeryProgressComponent { CompletedSteps = [$"{args.SurgeryProto}:{args.StepProto}"]};
            if(!args.IsFinal)
                progress.StartedSurgeries.Add(args.SurgeryProto);
            AddComp(args.Part, progress);
        }
        if (args.IsFinal)
            progress.CompletedSurgeries.Add(args.SurgeryProto);
    }
    private void OnStep(Entity<SurgeryStepComponent> ent, ref SurgeryStepEvent args)
    {
        foreach (var reg in (ent.Comp.Tools ?? []).Values)
        {
            var tool = args.Tools.FirstOrDefault(x => HasComp(x, reg.Component.GetType()));
            if (tool == default) return;

            if (_net.IsServer && TryComp(tool, out SurgeryToolComponent? toolComp) && toolComp.EndSound != null)
                _audio.PlayPvs(toolComp.EndSound, tool);
        }

        foreach (var reg in (ent.Comp.Add ?? []).Values)
        {
            var compType = reg.Component.GetType();
            if (HasComp(args.Part, compType))
                continue;
            var newComp = _compFactory.GetComponent(compType);
            _serialization.CopyTo(reg.Component, ref newComp, notNullableOverride: true);
            AddComp(args.Part, newComp);
        }

        foreach (var reg in (ent.Comp.BodyAdd ?? []).Values)
        {
            var compType = reg.Component.GetType();
            if (HasComp(args.Body, compType))
                continue;

            AddComp(args.Part, _compFactory.GetComponent(compType));
        }

        foreach (var reg in (ent.Comp.Remove ?? []).Values)
            RemComp(args.Part, reg.Component.GetType());

        foreach (var reg in (ent.Comp.BodyRemove ?? []).Values)
            RemComp(args.Body, reg.Component.GetType());
    }

    private void OnCanPerformStep(Entity<SurgeryStepComponent> ent, ref SurgeryCanPerformStepEvent args)
    {
        if (HasComp<SurgeryOperatingTableConditionComponent>(ent)
            && (!TryComp(args.Body, out BuckleComponent? buckle) || !HasComp<OperatingTableComponent>(buckle.BuckledTo)))
        {
            args.Invalid = StepInvalidReason.NeedsOperatingTable;
            return;
        }

        RaiseLocalEvent(args.Body, ref args);

        if (args.Invalid != StepInvalidReason.None)
            return;
        
        if (_inventory.TryGetContainerSlotEnumerator(args.Body, out var enumerator, args.TargetSlots))
        {
            var items = 0f;
            var total = 0f;
            while (enumerator.MoveNext(out var con))
            {
                total++;
                if (con.ContainedEntity != null)
                    items++;
            }

            if (items > 0)
            {
                args.Invalid = StepInvalidReason.Armor;
                args.Popup = $"You need to take off armor from patient to perform this step!";
                return;
            }
        }

        if (args.Invalid != StepInvalidReason.None || ent.Comp.Tools == null)
            return;

        foreach (var reg in ent.Comp.Tools.Values)
        {
            var tool = args.Tools.FirstOrDefault(x => HasComp(x, reg.Component.GetType()));
            if (tool == default)
            {
                args.Invalid = StepInvalidReason.MissingTool;

                if (reg.Component is ISurgeryToolComponent toolComp)
                    args.Popup = $"You need {toolComp.ToolName} to perform this step!";

                return;
            }
            else if (TryComp<ItemToggleComponent>(tool, out var togglable) && !togglable.Activated)
            {
                args.Invalid = StepInvalidReason.DisabledTool;

                if (reg.Component is ISurgeryToolComponent toolComp)
                    args.Popup = $"You need enable {toolComp.ToolName} to perform this step!";

                return;
            }
            else if (TryComp<SurgeryItemSizeConditionComponent>(ent, out var itemSizeComp) && TryComp<ItemComponent>(tool, out var item) && _item.GetSizePrototype(item.Size) > _item.GetSizePrototype(itemSizeComp.Size))
            {
                args.Invalid = StepInvalidReason.TooHigh;
                return;
            }

            args.ValidTools.Add(tool);
        }
    }

    private void OnSurgeryTargetStepChosen(Entity<SurgeryTargetComponent> ent, ref SurgeryStepChosenBuiMsg args)
    {
        var user = args.Actor;
        if (GetEntity(args.Entity) is not { Valid: true } body
            || GetEntity(args.Part) is not { Valid: true } targetPart
            || !IsSurgeryValid(body, targetPart, args.Surgery, args.Step, out var surgery, out var part, out var step)
            || !_entitySystem.TryGetSingleton(args.Step, out var stepEnt)
            || !TryComp(stepEnt, out SurgeryStepComponent? stepComp)
            || !CanPerformStep(user, body, part.Comp.PartType, step, true, out _, out _, out var validTools))
        {
            return;
        }
        if (!PreviousStepsComplete(body, part, surgery, args.Step) || IsStepComplete(part, args.Surgery, args.Step))
        {
            var progress = Comp<SurgeryProgressComponent>(part);
            Dirty(part, progress);
            RefreshUI(ent);
            return;
        }

        if (_net.IsServer && TryComp(step, out MetaDataComponent? meta))
        {
            var surgeonName = MetaData(user).EntityName;
            _popup.PopupEntity($"{surgeonName.ToLower()} starts {meta.EntityName.ToLower()}", part, PopupType.LargeCaution);
        }

        var duration = stepComp.Duration;
        float SmallestSuccessRate = 1f;

        foreach (var tool in validTools)
            if (TryComp(tool, out SurgeryToolComponent? toolComp))
            {
                duration *= toolComp.Speed;
                if (toolComp.StartSound != null) _audio.PlayPvs(toolComp.StartSound, tool);

                if(toolComp.SuccessRate < SmallestSuccessRate)
                    SmallestSuccessRate = toolComp.SuccessRate;
            }

        if (TryComp(body, out TransformComponent? xform))
            _rotateToFace.TryFaceCoordinates(user, _transform.GetMapCoordinates(body, xform).Position);

        var ev = new SurgeryDoAfterEvent(args.Surgery, args.Step, SmallestSuccessRate);
        var doAfter = new DoAfterArgs(EntityManager, user, duration, ev, body, part)
        {
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.SameTarget,
            ForceNet = true
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    public (Entity<SurgeryComponent> Surgery, int Step)? GetNextStep(EntityUid body, EntityUid part, EntityUid surgery) => GetNextStep(body, part, surgery, []);
    private (Entity<SurgeryComponent> Surgery, int Step)? GetNextStep(EntityUid body, EntityUid part, Entity<SurgeryComponent?> surgery, List<EntityUid> requirements)
    {
        if (!Resolve(surgery, ref surgery.Comp))
            return null;

        if (requirements.Contains(surgery))
            throw new ArgumentException($"Surgery {surgery} has a requirement loop: {string.Join(", ", requirements)}");

        requirements.Add(surgery);

        if (surgery.Comp.Requirement is { } requirementsIds)
        {
            foreach (var requirementId in requirementsIds)
            {
                if (!_entitySystem.TryGetSingleton(requirementId, out var requirement)
                    && GetNextStep(body, part, requirement, requirements) is { } requiredNext 
                    && IsSurgeryValid(body, part, requirementId, requiredNext.Surgery.Comp.Steps[requiredNext.Step], out _, out _, out _))
                    return requiredNext;
            }
        }

        if (!TryComp<SurgeryProgressComponent>(part, out var progress))
        {
            AddComp<SurgeryProgressComponent>(part);
            return ((surgery, surgery.Comp), 0);
        }
        var surgeryProto = Prototype(surgery);
        for (var i = 0; i < surgery.Comp.Steps.Count; i++)
            if (!progress.CompletedSteps.Contains($"{surgeryProto?.ID}:{surgery.Comp.Steps[i]}"))
                return ((surgery, surgery.Comp), i);

        return null;
    }

    public bool PreviousStepsComplete(EntityUid body, EntityUid part, Entity<SurgeryComponent> surgery, EntProtoId step)
    {
        if (surgery.Comp.Requirement is { } requirements)
        {
            foreach (var requirement in requirements)
            {
                if (!_entitySystem.TryGetSingleton(requirement, out var requiredEnt)
                    || !TryComp(requiredEnt, out SurgeryComponent? requiredComp) 
                    || !PreviousStepsComplete(body, part, (requiredEnt, requiredComp), step) 
                    && IsSurgeryValid(body, part, requirement, step, out _, out _, out _))
                    return false;
            }
        }

        foreach (var surgeryStep in surgery.Comp.Steps)
        {
            if (surgeryStep == step)
                break;

            if (Prototype(surgery.Owner) is not EntityPrototype surgProto || !IsStepComplete(part, surgProto.ID, surgeryStep))
                return false;
        }

        return true;
    }

    public bool CanPerformStep(EntityUid user, EntityUid body, BodyPartType part, EntityUid step, bool doPopup) => CanPerformStep(user, body, part, step, doPopup, out _, out _, out _);
    public bool CanPerformStep(EntityUid user, EntityUid body, BodyPartType part, EntityUid step, bool doPopup, out string? popup, out StepInvalidReason reason, out HashSet<EntityUid> validTools)
    {
        var slot = part switch
        {
            BodyPartType.Head => SlotFlags.HEAD | SlotFlags.MASK | SlotFlags.EYES,
            BodyPartType.Torso => SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING,
            BodyPartType.Arm => SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING,
            BodyPartType.Hand => SlotFlags.GLOVES,
            BodyPartType.Leg => SlotFlags.OUTERCLOTHING | SlotFlags.LEGS,
            BodyPartType.Foot => SlotFlags.FEET,
            BodyPartType.Tail => SlotFlags.NONE,
            BodyPartType.Other => SlotFlags.NONE,
            _ => SlotFlags.NONE
        };

        var check = new SurgeryCanPerformStepEvent(user, body, GetTools(user), slot);
        RaiseLocalEvent(step, ref check);
        popup = check.Popup;
        validTools = check.ValidTools;

        if (check.Invalid != StepInvalidReason.None)
        {
            if (doPopup && check.Popup != null)
                _popup.PopupEntity(check.Popup, user, PopupType.SmallCaution);

            reason = check.Invalid;
            return false;
        }

        reason = default;
        return true;
    }

    public bool IsStepComplete(EntityUid part, EntProtoId surgery, EntProtoId step)
    {
        if (TryComp<SurgeryProgressComponent>(part, out var comp))
            return comp.CompletedSteps.Contains($"{surgery}:{step}");
        AddComp<SurgeryProgressComponent>(part);
        return false;
    }
}
