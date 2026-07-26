using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.Stacks;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger that requires some way of 'using' (with default action) an artifact entity.
/// </summary>
public sealed partial class XATInteractWithSystem : BaseXATSystem<XATInteractWithComponent>
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XATInteractWithComponent, MapInitEvent>(OnMapInit);
        XATSubscribeDirectEvent<InteractUsingEvent>(OnInteractUsing);
        XATSubscribeDirectEvent<XATInteractWithDoAfterEvent>(OnInteractWithComplete);
    }

    /// <summary>
    /// Define required amount of valid entities to trigger
    /// This amount stays consistent on multiple triggers
    /// </summary>
    private void OnMapInit(Entity<XATInteractWithComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.MaxCount = ent.Comp.InteractionCount.Next(_random); // randomly decide count to decrement.
        ent.Comp.Count = ent.Comp.MaxCount; // define count amount.
        Dirty(ent);
    }

    /// <summary>
    /// Trigger the node if the entity used in interaction matches the whitelist.
    /// </summary>
    private void OnInteractUsing(Entity<XenoArtifactComponent> artifact, Entity<XATInteractWithComponent, XenoArtifactNodeComponent> node, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!_whitelistSystem.IsWhitelistPassOrNull(node.Comp1.Whitelist, args.Used)) // must be on the whitelist.
            return;

        if (!_toggle.IsActivated(args.Used)) // if the item can be toggled on, it should be.
            return;

        if (!_powerCell.HasActivatableCharge(args.Used, user: args.User, predicted: true)) // if the item can be powered, it should be.
            return;

        _audio.PlayPredicted(node.Comp1.StartTriggerSound, artifact.Owner, artifact.Owner);
        _doAfter.TryStartDoAfter(
            new DoAfterArgs(EntityManager, args.User, node.Comp1.InteractionTime, new XATInteractWithDoAfterEvent(GetNetEntity(node)),
            artifact.Owner, artifact.Owner, args.Used)
            {
                NeedHand = true,
                BreakOnMove = true
            });
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

        if (args.Used == null || !_powerCell.TryUseActivatableCharge(args.Used.Value, user: args.User)) // try to use charge if we can.
            return;

        _audio.PlayPredicted(node.Comp1.SuccessTriggerSound, artifact.Owner, artifact.Owner); // play on the artifact as the interacter may be deleted.

        var amount = _stack.GetCount(args.Used.Value); // count how much we're interacting with, gets 1 if not a stack.

        if (node.Comp1.DestroyAfter == true) // artifact consumes the item.
        {
            if (HasComp<StackComponent>(args.Used) && amount > node.Comp1.Count) // _stack.ReduceCount doesn't effect non-stack items.
                _stack.ReduceCount(args.Used.Value, node.Comp1.Count);
            else
                PredictedQueueDel(args.Used);
        }

        if (amount < node.Comp1.Count) // reduce the current required count by our amount
            node.Comp1.Count -= amount;
        else
            node.Comp1.Count = 0;

        Dirty(node);
        if (node.Comp1.Count > 0)
        {
            _popup.PopupEntity(Loc.GetString("interact-actifact-more"), artifact.Owner, args.User);
            return; // insufficient, still need to add more!
        }

        Trigger(artifact, node);
        node.Comp1.Count = node.Comp1.MaxCount; //reset after successful trigger, required amount is always the same.
        Dirty(node);

        args.Handled = true;
    }
}
