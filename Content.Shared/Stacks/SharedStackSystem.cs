using System.Numerics;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Nutrition;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Stacks;

// Partial for general system code and event handlers.
/// <summary>
/// System for handling entities which represent a stack of identical items, usually materials.
/// </summary>
[UsedImplicitly]
public abstract partial class SharedStackSystem : EntitySystem
{
    [Dependency] private IViewVariablesManager _vvm = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private EntityLookupSystem _entityLookup = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStorageSystem _storage = default!;

    [Dependency] private EntityQuery<StackComponent> _stackQuery;

    // TODO: These should be in the prototype.
    public static readonly int[] DefaultSplitAmounts = { 1, 5, 10, 20, 30, 50 };

    public override void Initialize()
    {
        base.Initialize();

        _vvm.GetTypeHandler<StackComponent>()
            .AddPath(nameof(StackComponent.Count), (_, comp) => comp.Count, SetCount);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _vvm.GetTypeHandler<StackComponent>()
            .RemovePath(nameof(StackComponent.Count));
    }

    #region Subscriptions

    [SubscribeLocalEvent]
    private void OnStackInteractUsing(Entity<StackComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!_stackQuery.TryComp(args.Used, out var recipientStack))
            return;

        // Transfer stacks from ground to hand
        if (!TryMergeStacks((ent.Owner, ent.Comp), (args.Used, recipientStack), out var transferred))
            return; // if nothing transferred, leave without a pop-up

        args.Handled = true;

        // interaction is done, the rest is just generating a pop-up

        var popupPos = args.ClickLocation;
        var userCoords = Transform(args.User).Coordinates;

        if (!popupPos.IsValid(EntityManager))
        {
            popupPos = userCoords;
        }

        switch (transferred)
        {
            case > 0:
                _popup.PopupCoordinates($"+{transferred}", popupPos, args.User);

                if (GetAvailableSpace(recipientStack) == 0)
                {
                    _popup.PopupCoordinates(Loc.GetString("comp-stack-becomes-full"),
                        popupPos.Offset(new Vector2(0, -0.5f)),
                        args.User);
                }

                break;

            case 0 when GetAvailableSpace(recipientStack) == 0:
                _popup.PopupCoordinates(Loc.GetString("comp-stack-already-full"), popupPos, args.User);
                break;
        }

        if (ent.Comp.AnimatePickup)
        {
            var localRotation = Transform(args.Used).LocalRotation;
            _storage.PlayPickupAnimation(args.Used, popupPos, userCoords, localRotation, args.User);
        }
    }

    [SubscribeLocalEvent]
    private void OnStackStarted(Entity<StackComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp(ent.Owner, out AppearanceComponent? appearance))
            return;

        _appearance.SetData(ent.Owner, StackVisuals.Actual, ent.Comp.Count, appearance);
        _appearance.SetData(ent.Owner, StackVisuals.MaxCount, GetMaxCount(ent.Comp), appearance);
        _appearance.SetData(ent.Owner, StackVisuals.Hide, false, appearance);
    }

    [SubscribeLocalEvent]
    private void OnStackGetState(Entity<StackComponent> ent, ref ComponentGetState args)
    {
        args.State = new StackComponentState(ent.Comp.Count, ent.Comp.MaxCountOverride, ent.Comp.Unlimited);
    }

    [SubscribeLocalEvent]
    private void OnStackHandleState(Entity<StackComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not StackComponentState cast)
            return;

        ent.Comp.MaxCountOverride = cast.MaxCountOverride;
        ent.Comp.Unlimited = cast.Unlimited;
        // This will change the count and call events.
        SetCount(ent.AsNullable(), cast.Count);
    }

    [SubscribeLocalEvent]
    private void OnStackExamined(Entity<StackComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(
            Loc.GetString("comp-stack-examine-detail-count",
                ("count", ent.Comp.Count),
                ("markupCountColor", "lightgray")
            )
        );
    }

    [SubscribeLocalEvent]
    private void OnBeforeEaten(Entity<StackComponent> eaten, ref BeforeIngestedEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Solution is not { } sol)
            return;

        // If the entity is empty and is a lingering entity we can't eat from it.
        if (eaten.Comp.Count <= 0)
        {
            args.Cancelled = true;
            return;
        }

        // If we've made it this far, we should refresh the solution when this item is eaten provided it's not the last one in the stack!
        args.Refresh = eaten.Comp.Count > 1;

        /*
        Edible stacked items is near completely evil so we must choose one of the following:
        - Option 1: Eat the entire solution each bite and reduce the stack by 1.
        - Option 2: Multiply the solution eaten by the stack size.
        - Option 3: Divide the solution consumed by stack size.
        The easiest and safest option is and always will be Option 1 otherwise we risk reagent deletion or duplication.
        That is why we cancel if we cannot set the minimum to the entire volume of the solution.
        */
        if (args.TryNewMinimum(sol.Volume))
            return;

        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnEaten(Entity<StackComponent> eaten, ref IngestedEvent args)
    {
        ReduceCount(eaten.AsNullable(), 1);
    }

    [SubscribeLocalEvent]
    private void OnStackAlternativeInteract(Entity<StackComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User; // Can't pass ref events into verbs

        var priority = 0;
        foreach (var amount in DefaultSplitAmounts)
        {
            if (amount > ent.Comp.Count)
                continue;

            AlternativeVerb verb = new()
            {
                Text = amount.ToString(),
                Category = VerbCategory.Split,
                Act = () => UserSplit(ent, user, amount),
                // we want to sort by size, not alphabetically by the verb text.
                Priority = priority,
            };

            priority--;

            args.Verbs.Add(verb);
        }

        var half = (ent.Comp.Count + 1) / 2;
        AlternativeVerb halve = new()
        {
            Text = Loc.GetString("comp-stack-split-halve"),
            Category = VerbCategory.Split,
            Act = () => UserSplit(ent, user, half),
            Priority = ent.Comp.HalfOnAltInteract ? 1 : priority - 1,
        };
        args.Verbs.Add(halve);
    }

    #endregion

    /// <summary>
    ///     Splits a stack and either mergers the stack into the hand of the user of picks it up into the hand of the user
    /// </summary>
    /// <param name="stack"> Stack which is to be split from </param>
    /// <param name="user"> The user who is trying to split the stack </param>
    /// <param name="amount"> Amount to try and split the stack. Will not always be the amount moved </param>
    /// <remarks>
    ///     OnStackAlternativeInteract() was moved to shared in order to faciliate prediction of stack splitting verbs.
    ///     However, prediction of interacitons with spawned entities is non-functional (or so i'm told)
    ///     So, UserSplit() and Split() should remain on the server for the time being.
    ///     This empty virtual method allows for UserSplit() to be called on the server from the client.
    ///     When prediction is improved, those two methods should be moved to shared, in order to predict the splitting itself (not just the verbs)
    /// </remarks>
    public void UserSplit(Entity<StackComponent> stack, EntityUid user, int amount)
    {
        if (amount <= 0)
        {
            _popup.PopupCursor(Loc.GetString("comp-stack-split-too-small"), user, PopupType.Medium);
            return;
        }

        // Tries to merge stack with a stack in hand.
        if (_hands.TryGetActiveItem(user, out var merger)
            && TryMergeStacks(stack.AsNullable(), merger.Value, out _, amount: amount))
        {
            _popup.PopupCursor(Loc.GetString("comp-stack-split"), user);
            return;
        }

        // If this is effectively just picking up the stack, it just picks up the stack.
        if (stack.Comp.Count <= amount)
        {
            // No AnimatePickup passed as this is a normal pickup.
            _hands.PickupOrDrop(user, stack.Owner);
            return;
        }

        if (Split(stack.AsNullable(), amount, new EntityCoordinates(user, Vector2.Zero), user: user) is not { } split)
            return;

        _hands.PickupOrDrop(user, split, animate: stack.Comp.AnimatePickup);
        _popup.PopupCursor(Loc.GetString("comp-stack-split"), user);
    }

    /// <summary>
    /// Spawns a new entity and moves an amount to it from the stack.
    /// Moves nothing if amount is greater than ent's stack count.
    /// </summary>
    /// <param name="ent">Entity to split in a new stack.</param>
    /// <param name="amount">How much to move to the new entity.</param>
    /// <param name="spawnPosition">Where to spawn the new stack</param>
    /// <returns>Null if StackComponent doesn't resolve, or amount to move is greater than ent has available.</returns>
    [PublicAPI]
    public virtual EntityUid? Split(Entity<StackComponent?> ent, int amount, EntityCoordinates spawnPosition, EntityUid? user = null)
    {
        return null;
    }
}

/// <summary>
/// Event raised when a stack's count has changed.
/// </summary>
public sealed class StackCountChangedEvent : EntityEventArgs
{
    /// <summary>
    /// The old stack count.
    /// </summary>
    public int OldCount;

    /// <summary>
    /// The new stack count.
    /// </summary>
    public int NewCount;

    public StackCountChangedEvent(int oldCount, int newCount)
    {
        OldCount = oldCount;
        NewCount = newCount;
    }
}
