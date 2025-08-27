using Content.Server.Popups;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Stacks;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Server.Stack;

/// <summary>
/// Adds context-menu verbs to stack items:
/// - Split 1 to hand
/// - Split 5 to hand (if count >= 6)
/// - Split half to hand
///
/// One file only; no prototype changes required.
/// </summary>
[RegisterSystem]
public sealed class StackSplitVerbsSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        // Provide verbs for any entity with a StackComponent.
        SubscribeLocalEvent<StackComponent, GetVerbsEvent<UtilityVerb>>(OnGetUtilityVerbs);
    }

    private void OnGetUtilityVerbs(EntityUid uid, StackComponent stack, ref GetVerbsEvent<UtilityVerb> args)
    {
        // Discoverable, but only when actionable.
        if (!args.CanAccess || !args.CanInteract || args.User == default || stack.Count <= 1)
            return;

        AddSplitVerb(uid, ref args, 1, "Split 1 to hand");

        if (stack.Count >= 6)
            AddSplitVerb(uid, ref args, 5, "Split 5 to hand");

        var half = stack.Count / 2;
        if (half >= 1)
            AddSplitVerb(uid, ref args, half, $"Split {half} (half) to hand");
    }

    private void AddSplitVerb(EntityUid uid, ref GetVerbsEvent<UtilityVerb> args, int amount, string text)
    {
        args.Verbs.Add(new UtilityVerb
        {
            Text = text,                  // TODO: localize (optional)
            Category = VerbCategory.Manage,
            Act = () => SplitToHand(uid, amount, args.User)
        });
    }

    /// <summary>
    /// Splits <paramref name="amount"/> off <paramref name="uid"/> and tries to put it in <paramref name="user"/>'s hand.
    /// Drops at feet if hands are full.
    /// </summary>
    private void SplitToHand(EntityUid uid, int amount, EntityUid user)
    {
        if (!TryComp<StackComponent>(uid, out var srcStack) || amount <= 0)
            return;

        if (!CanDoInteract(user, uid))
            return;

        if (srcStack.Count <= amount)
        {
            _popup.PopupEntity("Not enough in the stack.", user);
            return;
        }

        if (!TrySplitStack(uid, amount, out var newStackUid))
        {
            _popup.PopupEntity("Could not split the stack.", user);
            return;
        }

        // Try to place into user's hands; if not, place at their feet.
        TryComp(user, out HandsComponent? hands);
        if (!_hands.TryPickup(user, newStackUid.Value, handsComp: hands))
        {
            var userXform = Transform(user);
            var newXform = Transform(newStackUid.Value);
            newXform.Coordinates = userXform.Coordinates;
        }
    }

    /// <summary>
    /// Conservative manual split:
    /// spawns an identical entity to the source and transfers the count.
    /// Uses only server-safe APIs so it works regardless of StackSystem helpers in your checkout.
    /// </summary>
    private bool TrySplitStack(EntityUid srcUid, int amount, out EntityUid? newUid)
    {
        newUid = null;

        if (!TryComp<StackComponent>(srcUid, out var srcStack) || srcStack.Count <= amount)
            return false;

        if (!TryComp<MetaDataComponent>(srcUid, out var meta) || meta.EntityPrototype == null)
            return false;

        var coords = Transform(srcUid).Coordinates;

        // Spawn same prototype as the source.
        var spawned = Spawn(meta.EntityPrototype.ID, coords);
        if (!TryComp<StackComponent>(spawned, out var newStack))
        {
            QueueDel(spawned);
            return false;
        }

        // Update counts.
        srcStack.Count -= amount;
        Dirty(srcUid, srcStack);

        newStack.Count = amount;
        Dirty(spawned, newStack);

        newUid = spawned;
        return true;
    }

    private static bool CanDoInteract(EntityUid user, EntityUid target)
    {
        if (user == default || target == default)
            return false;

        // Disallow interactions across unrelated containers.
        if (ContainerHelpers.IsInContainer(target) && !ContainerHelpers.IsInSameOrParentContainer(user, target))
            return false;

        return true;
    }
}
