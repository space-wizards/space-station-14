using System.Numerics;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Nutrition;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Stacks
{
    // Partial for general system code and event handlers.
    /// <summary>
    /// System for handling entities which represent a stack of identical items, usually materials.
    /// </summary>
    [UsedImplicitly]
    public abstract partial class SharedStackSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly IViewVariablesManager _vvm = default!;
        [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
        [Dependency] protected readonly SharedHandsSystem Hands = default!;
        [Dependency] protected readonly SharedTransformSystem Xform = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] protected readonly SharedPopupSystem Popup = default!;
        [Dependency] private readonly SharedStorageSystem _storage = default!;

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

            _vvm.GetTypeHandler<StackComponent>()
                .AddPath(nameof(StackComponent.Count), (_, comp) => comp.Count, SetCount);
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
            UpdateLingering(ent);

            if (!TryComp(ent.Owner, out AppearanceComponent? appearance))
                return;

            Appearance.SetData(ent.Owner, StackVisuals.Actual, ent.Comp.Count, appearance);
            Appearance.SetData(ent.Owner, StackVisuals.MaxCount, GetMaxCount(ent.Comp), appearance);
            Appearance.SetData(ent.Owner, StackVisuals.Hide, false, appearance);
        }

        /// <summary>
        ///     Used on client to set visuals for a lingering stack.
        /// </summary>
        protected virtual void UpdateLingering(Entity<StackComponent> ent)
        {
        }

        private void OnStackGetState(Entity<StackComponent> ent, ref ComponentGetState args)
        {
            args.State = new StackComponentState(ent.Comp.Count, ent.Comp.MaxCountOverride, ent.Comp.Lingering);
        }

        private void OnStackHandleState(Entity<StackComponent> ent, ref ComponentHandleState args)
        {
            if (args.Current is not StackComponentState cast)
                return;

            ent.Comp.MaxCountOverride = cast.MaxCount;
            ent.Comp.Lingering = cast.Lingering;
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

            /*
            Edible stacked items is near completely evil so we must choose one of the following:
            - Option 1: Eat the entire solution each bite and reduce the stack by 1.
            - Option 2: Multiply the solution eaten by the stack size.
            - Option 3: Divide the solution consumed by stack size.
            The easiest and safest option is and always will be Option 1 otherwise we risk reagent deletion or duplication.
            That is why we cancel if we cannot set the minimum to the entire volume of the solution.
            */
            if(args.TryNewMinimum(sol.Volume))
                return;

            args.Cancelled = true;
        }

        private void OnEaten(Entity<StackComponent> eaten, ref IngestedEvent args)
        {
            if (!TryUse(eaten.AsNullable(), 1))
                return;

            // We haven't eaten the whole stack yet or are unable to eat it completely.
            if (eaten.Comp.Count > 0 || eaten.Comp.Lingering)
            {
                args.Refresh = true;
                return;
            }

            // Here to tell the food system to do destroy stuff.
            args.Destroy = true;
        }
    }

    /// <summary>
    ///     Event raised when a stack's count has changed.
    /// </summary>
    public sealed class StackCountChangedEvent : EntityEventArgs
    {
        /// <summary>
        ///     The old stack count.
        /// </summary>
        public int OldCount;

        /// <summary>
        ///     The new stack count.
        /// </summary>
        public int NewCount;

        public StackCountChangedEvent(int oldCount, int newCount)
        {
            OldCount = oldCount;
            NewCount = newCount;
        }
    }
}
