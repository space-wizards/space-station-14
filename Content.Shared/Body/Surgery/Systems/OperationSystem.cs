using Content.Shared.Body.Part;
using Content.Shared.Body.Surgery.Components;
using Content.Shared.Body.Surgery.Operation;
using Content.Shared.Body.Surgery.Operation.Step;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.Body.Surgery.Systems;

public sealed class OperationSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSurgerySystem _surgery = default!;

    public IEnumerable<SurgeryOperationPrototype> AllSurgeries =>
        _proto.EnumeratePrototypes<SurgeryOperationPrototype>().Where(op => !op.Hidden);
        // TODO: prerequisites like having ribcage/skull opened or for adding limbs, missing a limb

    public IEnumerable<SurgeryOperationPrototype> PossibleSurgeries(BodyPartComponent part)
    {
        return AllSurgeries.Where(op => op.Parts.Contains(part.PartType));
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OperationComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<OperationComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnGetState(EntityUid uid, OperationComponent comp, ref ComponentGetState args)
    {
        args.State = new OperationComponentState(comp.Part, comp.Drapes, comp.Prototype!.ID, comp.SelectedOrgan);
    }

    private void OnHandleState(EntityUid uid, OperationComponent comp, ref ComponentHandleState args)
    {
        if (args.Current is not OperationComponentState state)
            return;

        comp.Part = state.Part;
        comp.Drapes = state.Drapes;
        comp.Prototype = _proto.Index<SurgeryOperationPrototype>(state.Prototype);
        comp.SelectedOrgan = state.SelectedOrgan;
    }

    /// <summary>
    /// Start a new operation on an entity.
    /// </summary>
    /// <returns>true if starting it succeeds, false otherwise</returns>
    public bool StartOperation(EntityUid uid, EntityUid part, EntityUid drapes, string id, [NotNullWhen(true)] out OperationComponent? comp)
    {
        if (_proto.TryIndex<SurgeryOperationPrototype>(id, out var prototype))
        {
            comp = AddComp<OperationComponent>(uid);
            comp.Part = part;
            comp.Drapes = drapes;
            comp.Prototype = prototype;
            return true;
        }

        comp = null;
        Logger.WarningS("surgery", $"Unknown surgery prototype '{id}' to be used on {ToPrettyString(uid):target}'s {ToPrettyString(part):part}");
        return false;
    }

    /// <summary>
    /// Try to insert an item for a step
    /// </summary>
    public bool TryInsertItem(OperationComponent comp, EntityUid item)
    {
        var step = GetNextStep(comp);
        return step?.InsertionHandler?.TryInsert(comp.Part, item) ?? false;
    }

    /// <summary>
    /// Returns the next step for this operation.
    /// </summary>
    private OperationStep? GetNextStep(OperationComponent comp)
    {
        var steps = comp.Prototype!.Steps;
        if (steps.Count <= comp.Tags.Count)
            return null;

        return steps[comp.Tags.Count];
    }

    public bool CanPerform(
        EntityUid target,
        EntityUid surgeon,
        OperationComponent comp,
        SurgeryToolComponent tool,
        [NotNullWhen(true)] out OperationStep? step)
    {
        step = GetNextStep(comp);
        if (step == null)
            return false;

        var context = new SurgeryStepContext(target, surgeon, comp, tool, step.ID, this, _surgery);
        return step.CanPerform(context);
    }

    /// <summary>
    /// Use a tool in an operation, if possible.
    /// </summary>
    public bool TryPerform(
        EntityUid target,
        EntityUid surgeon,
        OperationComponent comp,
        EntityUid toolId,
        SurgeryToolComponent tool)
    {
        var step = GetNextStep(comp);
        if (step == null)
            return false;

        var context = new SurgeryStepContext(target, surgeon, comp, tool, step.ID, this, _surgery);
        if (step.CanPerform(context) && step.Perform(context))
        {
            Logger.InfoS("surgery", $"{ToPrettyString(surgeon):player} completed step {step.ID} of {comp.Prototype!.Name} on {ToPrettyString(target):target}");
            step.OnPerformSuccess(context);
            return true;
        }

        // if using a cautery, stop the operation regardless of stage
        if (HasComp<CauteryComponent>(toolId))
        {
            Logger.InfoS("surgery", $"{ToPrettyString(surgeon):player} aborted operation {comp.Prototype!.Name} on {ToPrettyString(target):target}");
            var userName = Identity.Name(surgeon, EntityManager);
            var targetName = Identity.Name(target, EntityManager);
            Popup(Loc.GetString("surgery-aborted", ("user", userName), ("target", targetName)), surgeon);
            RemComp<OperationComponent>(target);
            return true;
        }

        step.OnPerformFail(context);
        return false;
    }

    public bool CanAddSurgeryTag(OperationComponent comp, SurgeryTag tag)
    {
        // TODO SURGERY: fix this for intermediary unnecessary steps
        // probably needs a while loop not sure
        var step = GetNextStep(comp);
        return (step?.Necessary(comp) ?? false) && step.ID == tag.ID;
    }

    public void AddSurgeryTag(EntityUid user, EntityUid uid, OperationComponent comp, SurgeryTag tag)
    {
        comp.Tags.Add(tag);
        Dirty(comp);

        CheckCompletion(user, uid, comp);
    }

    public bool TryRemoveSurgeryTag(OperationComponent comp, SurgeryTag tag)
    {
        if (comp.Tags.Count == 0 ||
            comp.Tags[^1] != tag)
        {
            return false;
        }

        comp.Tags.RemoveAt(comp.Tags.Count - 1);
        Dirty(comp);

        return true;
    }

    private void CheckCompletion(EntityUid user, EntityUid uid, OperationComponent comp)
    {
        if (comp.Prototype!.Steps.Count > comp.Tags.Count)
            return;

        var offset = 0;

        for (var i = 0; i < comp.Tags.Count; i++)
        {
            var step = comp.Prototype.Steps[i + offset];

            if (!step.Necessary(comp))
            {
                offset++;
                step = comp.Prototype.Steps[i + offset];
            }

            if (comp.Tags[i] != step.ID)
                return;
        }

        Logger.InfoS("surgery", $"{ToPrettyString(user):player} completed operation {comp.Prototype!.Name} on {ToPrettyString(comp.Part):part}");
        comp.Prototype.Effect?.Execute(user, comp);
        RemComp<OperationComponent>(uid);
    }

    /// <summary>
    /// Sets the operation's SelectedOrgan field.
    /// </summary>
    public void SelectOrgan(OperationComponent comp, EntityUid? organ)
    {
        comp.SelectedOrgan = organ;
        Dirty(comp);
    }

    /// <summary>
    /// Prevents more doafters from starting until current one ends, if true.
    /// </summar>
    public void SetBusy(OperationComponent comp, bool busy)
    {
        comp.Busy = busy;
        Dirty(comp);
    }

    public void DoBeginPopups(EntityUid surgeon, EntityUid target, EntityUid part, string id)
    {
        DoPopups(surgeon, target, part, id, "begin");
    }

    public void DoSuccessPopups(EntityUid surgeon, EntityUid target, EntityUid part, string id)
    {
        DoPopups(surgeon, target, part, id, "success");
    }

    private void DoPopups(EntityUid user, EntityUid target, EntityUid part, string id, string type)
    {
        id = id.ToLowerInvariant();

        var action = Loc.GetString($"surgery-step-{id}-{type}");
        // if the targeted entity is the same as the operated-on bodypart, there is no specific area being operated on
        // i.e. doing surgery on a grape or a severed limb
        id = (part == target)
            ? $"surgery-step-{type}-no-zone-popup"
            : (user == target)
                ? $"surgery-step-{type}-self-popup"
                : $"surgery-step-{type}-popup";
        var userName = Identity.Name(user, EntityManager);
        var targetName = Identity.Name(target, EntityManager);
        var msg = Loc.GetString(id, ("user", userName), ("action", action), ("target", targetName), ("part", part));
        PopupAll(msg, user);
    }

    public void Popup(string msg, EntityUid user)
    {
        _popup.PopupEntity(msg, user);
    }

    public void PopupAll(string msg, EntityUid user)
    {
        _popup.PopupEntity(msg, user, user);
    }
}
