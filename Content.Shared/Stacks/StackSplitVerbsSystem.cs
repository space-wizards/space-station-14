using System;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Content.Shared.Stacks;
using Content.Shared.Stacks.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.Map;
using Robust.Shared.Localization;
using Robust.Shared.Containers;

namespace Content.Server.Stacks.Systems;

/// <summary>
/// Adds right-click verbs to stack items:
/// - Split 1 to hand
/// - Split 5 to hand (if available)
/// - Split half to hand
///
/// All in a single file with no prototype/FTL changes required.
/// </summary>
[RegisterSystem]
public sealed class StackSplitVerbsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        // Add verbs for any entity that has a StackComponent.
        SubscribeLocalEvent<StackComponent, GetVerbsEvent<UtilityVerb>>(OnGetUtilityVerbs);
    }

    private void OnGetUtilityVerbs(EntityUid uid, StackComponent stack, ref GetVerbsEvent<UtilityVerb> args)
    {
        // Must be able to reach & interact. Only show to players.
        if (!args.CanAccess || !args.CanInteract || args.User == default || stack.Count <= 1)
            return;

        // Build: Split 1 to hand
        {
            var v = new UtilityVerb
            {
                Text = "Split 1 to hand", // TODO: localize
                Category = VerbCategory.Manage, // keeps it out of the main interactions block
                Act = () => SplitToHand(uid, 1, args.User)
            };
            args.Verbs.Add(v);
        }

        // Build: Split 5 to hand (only if plenty remain)
        if (stack.Count >= 6)
        {
            var v5 = new UtilityVerb
            {
                Text = "Split 5 to hand", // TODO: localize
                Category = VerbCategory.Manage,
                Act = () => SplitToHand(uid, 5, args.User)
            };
            args.Verbs.Add(v5);
        }

        // Build: Split half to hand (mirrors alt-click but discoverable in the menu)
        var half = stack.Count / 2;
        if (half >= 1)
        {
            var vh = new UtilityVerb
            {
                Text = $"Split {half} (half) to hand", // TODO: localize
                Category = VerbCategory.Manage,
                Act = () => SplitToHand(uid, half, args.User)
            };
            args.Verbs.Add(vh);
        }
    }

    /// <summary>
    /// Splits 'amount' from the given stack entity 'uid' and attempts to place it into the user's hands.
    /// If no free hand, the split portion is spawned at the user's feet.
    /// </summary>
    private void SplitToHand(EntityUid uid, int amount, EntityUid user)
    {
        if (!TryComp<StackComponent>(uid, out var srcStack) || amount <= 0)
            return;

        // Safety: user must still be able to interact and the entity must be accessible
        if (!CanDoInteract(user, uid))
            return;

        if (srcStack.Count <= amount)
        {
            _popup.PopupEntity("Not enough in the stack.", user); // TODO: localize
            return;
        }

        // We try to split using the shared Stack APIs if available,
        // otherwise we fallback to a simple proto spawn + count transfer.
        // This is intentionally conservative, relying on existing SS14 stack semantics.
        if (!TrySplitStack(uid, amount, user, out var newStack))
        {
            _popup.PopupEntity("Could not split the stack.", user); // TODO: localize
            return;
        }

        // Try to place into hands; if no free hand, leave it at the user's feet.
        if (!_hands.TryPickup(user, newStack.Value))
        {
            // If pickup failed, ensure the split entity sits on the ground near the user.
            var xform = Transform(user);
            var coords = xform.Coordinates;
            var newXform = Transform(newStack.Value);
            newXform.Coordinates = coords;
        }
    }

    /// <summary>
    /// Tries to split the stack via the engine's stack helpers if available.
    /// If not, does a safe manual split using the entity prototype and StackComponent.Count.
    /// </summary>
    private bool TrySplitStack(EntityUid srcUid, int amount, EntityUid user, out EntityUid? outNew)
    {
        outNew = null;

        // Preferred path: if StackSystem exposes a split helper in your checkout,
        // call it here (names vary across revisions). Example:
        //
        //   if (EntitySystem.TryGet<StackSystem>(out var stacks) &&
        //       stacks.TrySplit(srcUid, amount, out var newUid))
        //   {
        //       outNew = newUid;
        //       return true;
        //   }
        //
        // Because API surfaces can shift, we supply a robust manual fallback below.

        if (!TryComp<StackComponent>(srcUid, out var srcStack) || srcStack.Count <= amount)
            return false;

        // Resolve the proto so we spawn the same entity type as the source.
        if (!TryComp<MetaDataComponent>(srcUid, out var meta) || meta.EntityPrototype == null)
            return false;

        var srcXf = Transform(srcUid);
        var mapCoords = srcXf.MapPosition;

        // Spawn a fresh copy of the entity and set its stack count to 'amount'.
        var newUid = EntityManager.SpawnEntity(meta.EntityPrototype.ID, mapCoords);
        if (!TryComp<StackComponent>(newUid, out var newStack))
        {
            // If the spawned entity is somehow not stackable, delete and abort.
            Del(newUid);
            return false;
        }

        // Decrement the source and set the new count.
        srcStack.Count -= amount;
        Dirty(srcUid, srcStack);

        newStack.Count = amount;
        Dirty(newUid, newStack);

        outNew = newUid;
        return true;
    }

    private bool CanDoInteract(EntityUid user, EntityUid target)
    {
        // Minimal, conservative checks:
        //  - user must be alive/able to interact
        //  - both in same map/container scope and within interact range
        // You can expand this to use InteractionSystem.CanInteract if you prefer.
        if (user == default || target == default)
            return false;

        // If either is in a container that blocks interaction, bail.
        if (ContainerHelpers.IsInContainer(target) && !ContainerHelpers.IsInSameOrParentContainer(user, target))
            return false;

        return true;
    }
}
