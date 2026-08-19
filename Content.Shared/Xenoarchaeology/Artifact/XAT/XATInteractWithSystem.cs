using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger that requires some way of 'using' (with default action) an artifact entity.
/// </summary>
public sealed partial class XATInteractWithSystem : BaseXATSystem<XATInteractWithComponent>
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        XATSubscribeDirectEvent<InteractUsingEvent>(OnInteractUsing);
        XATSubscribeDirectEvent<XATInteractWithDoAfterEvent>(OnInteractWithComplete);
    }

    /// <summary>
    /// Define required amount of valid entities to trigger
    /// This amount stays consistent on multiple triggers
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMapInit(Entity<XATInteractWithComponent> ent, ref MapInitEvent args)
    {
        SetMaxCount(ent);
    }

    /// <summary>
    /// Check against the whitelist. If conditions are met, begin doafter.
    /// </summary>
    private void OnInteractUsing(Entity<XenoArtifactComponent> artifact, Entity<XATInteractWithComponent, XenoArtifactNodeComponent> node, ref InteractUsingEvent args)
    {
        if (!_whitelistSystem.IsWhitelistPassOrNull(node.Comp1.Whitelist, args.Used)) // must be on the whitelist.
            return;

        _audio.PlayPredicted(node.Comp1.StartTriggerSound, artifact.Owner, args.User);

        _doAfter.TryStartDoAfter(
            new DoAfterArgs(
                EntityManager,
                args.User,
                node.Comp1.InteractionTime,
                new XATInteractWithDoAfterEvent(GetNetEntity(node)),
                artifact.Owner,
                artifact.Owner,
                args.Used
            )
            {
                NeedHand = true,
                BreakOnMove = true
            }
        );
    }

    /// <summary>
    /// Check our nodes match and nothing was cancelled. Then trigger.
    /// If the item is destroyed on use, destroy it (considering stacks).
    /// If there needs to be multiple, count towards it (considering stacks).
    /// </summary>
    private void OnInteractWithComplete(Entity<XenoArtifactComponent> artifact, Entity<XATInteractWithComponent, XenoArtifactNodeComponent> node, ref XATInteractWithDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (GetEntity(args.Node) != node.Owner)
            return;

        if (args.Used == null)
            return;

        if (node.Comp1.MaxCount == null || node.Comp1.Count == null) // if MaxCount or Count somehow are not set, reset them
            SetMaxCount((node.Owner, node.Comp1));

        _audio.PlayPredicted(node.Comp1.SuccessTriggerSound, artifact.Owner, args.User); // play on the artifact as the interactor may be deleted.

        var amount = _stack.GetCount(args.Used.Value); // count how much we're interacting with, gets 1 if not a stack.

        if (node.Comp1.DestroyAfter) // artifact consumes the item.
        {
            if (HasComp<StackComponent>(args.Used) && amount > node.Comp1.Count) // _stack.ReduceCount doesn't affect non-stack items.
                _stack.ReduceCount(args.Used.Value, node.Comp1.Count.Value);
            else
                PredictedQueueDel(args.Used);
        }

        node.Comp1.Count -= amount; // reduce the current required count by our amount

        if (node.Comp1.Count > 0)
        {
            if (node.Comp1.InsufficientInteractionPopup != null)
                _popup.PopupEntity(Loc.GetString(node.Comp1.InsufficientInteractionPopup), artifact.Owner, args.User);
            Dirty(node);
            return; // insufficient, still need to add more!
        }

        Trigger(artifact, node);
        node.Comp1.Count = node.Comp1.MaxCount; //reset after successful trigger, required amount is always the same.
        Dirty(node);

        args.Handled = true;
    }

    private void SetMaxCount(Entity<XATInteractWithComponent> ent)
    {
        if (ent.Comp.MaxCount == null)
            ent.Comp.MaxCount = ent.Comp.InteractionCount.Next(_random); // randomly decide count to decrement.

        if (ent.Comp.Count == null)
            ent.Comp.Count = ent.Comp.MaxCount.Value; // define count amount.

        Dirty(ent);
    }
}


