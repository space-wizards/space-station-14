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
using Robust.Shared.Prototypes;

namespace Content.Shared.Stacks;

// Partial for general system code and event handlers.
/// <summary>
/// System for handling entities which represent a stack of identical items, usually materials.
/// This is a good example for learning how to code in an ECS manner.
/// </summary>
[UsedImplicitly]
public abstract partial class SharedStackSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IViewVariablesManager _vvm = default!;
    [Dependency] protected SharedAppearanceSystem Appearance = default!;
    [Dependency] protected SharedHandsSystem Hands = default!;
    [Dependency] protected SharedTransformSystem Xform = default!;
    [Dependency] private EntityLookupSystem _entityLookup = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] private SharedStorageSystem _storage = default!;

    // TODO: These should be in the prototype.
    public static readonly int[] DefaultSplitAmounts = { 1, 5, 10, 20, 30, 50 };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StackComponent, InteractUsingEvent>(OnStackInteractUsing);
        SubscribeLocalEvent<StackComponent, ComponentGetState>(OnStackGetState);
        SubscribeLocalEvent<StackComponent, ComponentHandleState>(OnStackHandleState);
        SubscribeLocalEvent<StackComponent, ComponentStartup>(OnStackStarted);
        SubscribeLocalEvent<StackComponent, ExaminedEvent>(OnStackExamined);

        SubscribeLocalEvent<StackComponent, BeforeIngestedEvent>(OnBeforeEaten);
        SubscribeLocalEvent<StackComponent, IngestedEvent>(OnEaten);
        SubscribeLocalEvent<StackComponent, GetVerbsEvent<AlternativeVerb>>(OnStackAlternativeInteract);

        _vvm.GetTypeHandler<StackComponent>()
            .AddPath(nameof(StackComponent.Count),
                (_, comp) => comp.Count,
                (uid, value, comp) => SetCount((uid, comp), value));
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _vvm.GetTypeHandler<StackComponent>()
            .RemovePath(nameof(StackComponent.Count));
    }

    private void OnStackInteractUsing(Entity<StackComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<StackComponent>(args.Used, out var recipientStack))
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
                Popup.PopupClient($"+{transferred}", popupPos, args.User);

                if (GetAvailableSpace(recipientStack) == 0)
                {
                    Popup.PopupClient(Loc.GetString("comp-stack-becomes-full"),
                        popupPos.Offset(new Vector2(0, -0.5f)),
                        args.User);
                }

                break;

            case 0 when GetAvailableSpace(recipientStack) == 0:
                Popup.PopupClient(Loc.GetString("comp-stack-already-full"), popupPos, args.User);
                break;
        }

        var localRotation = Transform(args.Used).LocalRotation;
        _storage.PlayPickupAnimation(args.Used, popupPos, userCoords, localRotation, args.User);
    }

    private void OnStackStarted(Entity<StackComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp(ent.Owner, out AppearanceComponent? appearance))
            return;

        Appearance.SetData(ent.Owner, StackVisuals.Actual, ent.Comp.Count, appearance);
        Appearance.SetData(ent.Owner, StackVisuals.MaxCount, GetMaxCount(ent.Comp), appearance);
        Appearance.SetData(ent.Owner, StackVisuals.Hide, false, appearance);
    }

    private void OnStackGetState(Entity<StackComponent> ent, ref ComponentGetState args)
    {
        args.State = new StackComponentState(ent.Comp.Count, ent.Comp.MaxCountOverride, ent.Comp.Unlimited);
    }

    private void OnStackHandleState(Entity<StackComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not StackComponentState cast)
            return;

        ent.Comp.MaxCountOverride = cast.MaxCountOverride;
        ent.Comp.Unlimited = cast.Unlimited;
        // This will change the count and call events.
        SetCount(ent.AsNullable(), cast.Count);
    }

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

    private void OnEaten(Entity<StackComponent> eaten, ref IngestedEvent args)
    {
        ReduceCount(eaten.AsNullable(), 1);
    }

    private void OnStackAlternativeInteract(Entity<StackComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || ent.Comp.Count == 1)
            return;

        var user = args.User; // Can't pass ref events into verbs

        AlternativeVerb halve = new()
        {
            Text = Loc.GetString("comp-stack-split-halve"),
            Category = VerbCategory.Split,
            Act = () => UserSplit(ent, user, ent.Comp.Count / 2),
            Priority = 1
        };
        args.Verbs.Add(halve);

        var priority = 0;
        foreach (var amount in DefaultSplitAmounts)
        {
            if (amount >= ent.Comp.Count)
                continue;

            AlternativeVerb verb = new()
            {
                Text = amount.ToString(),
                Category = VerbCategory.Split,
                Act = () => UserSplit(ent, user, amount),
                // we want to sort by size, not alphabetically by the verb text.
                Priority = priority
            };

            priority--;

            args.Verbs.Add(verb);
        }
    }

    protected void UserSplit(Entity<StackComponent> stack, Entity<TransformComponent?> user, int amount)
    {
        if (!Resolve(user.Owner, ref user.Comp, false))
            return;

        if (amount <= 0)
        {
            Popup.PopupClient(Loc.GetString("comp-stack-split-too-small"), user.Owner, PopupType.Medium);
            return;
        }

        if (Split(stack.AsNullable(), amount, user.Comp.Coordinates) is not { } split)
            return;

        Hands.PickupOrDrop(user.Owner, split);

        Popup.PopupClient(Loc.GetString("comp-stack-split"), user.Owner);
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
    public EntityUid? Split(Entity<StackComponent?> ent, int amount, EntityCoordinates spawnPosition)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return null;

        // Try to remove the amount of things we want to split from the original stack...
        if (!TryUse(ent, amount))
            return null;

        if (!_prototype.Resolve(ent.Comp.StackTypeId, out var stackType))
            return null;

        // Set the output parameter in the event instance to the newly split stack.
        var newEntity = PredictedSpawnAtPosition(stackType.Spawn, spawnPosition);

        // There should always be a StackComponent
        var stackComp = Comp<StackComponent>(newEntity);

        SetCount((newEntity, stackComp), amount);
        stackComp.Unlimited = false; // Don't let people dupe unlimited stacks
        Dirty(newEntity, stackComp);

        var ev = new StackSplitEvent(newEntity);
        RaiseLocalEvent(ent, ref ev);

        return newEntity;
    }

    /// <summary>
    /// Calculates how many stacks to spawn that total up to <paramref name="amount"/>.
    /// </summary>
    /// <returns>The list of stack counts per entity.</returns>
    private List<int> CalculateSpawns(int maxCountPerStack, int amount)
    {
        var amounts = new List<int>();
        while (amount > 0)
        {
            var countAmount = Math.Min(maxCountPerStack, amount);
            amount -= countAmount;
            amounts.Add(countAmount);
        }

        return amounts;
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
