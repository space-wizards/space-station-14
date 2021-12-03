using System;
using System.Linq;
using Content.Server.Buckle.Components;
using Content.Server.Hands.Components;
using Content.Server.Interaction;
using Content.Shared.ActionBlocker;
using Content.Shared.Coordinates;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Foldable
{
    [UsedImplicitly]
    public sealed class FoldableSystem : EntitySystem
    {
        [Dependency] private readonly InteractionSystem _interactionSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FoldableComponent, InteractHandEvent>(OnInteract);
            SubscribeLocalEvent<FoldableComponent, AttemptItemPickupEvent>(OnPickedUpAttempt);

            SubscribeLocalEvent<FoldableComponent, GetInteractionVerbsEvent>(AddFoldVerb);
        }

        private bool TryToggleFold(FoldableComponent comp)
        {
            return TrySetFolded(comp, !comp.IsFolded);
        }

        /// <summary>
        /// Try to fold/unfold
        /// </summary>
        /// <param name="comp"></param>
        /// <param name="state">Folded state we want</param>
        /// <returns>True if successful</returns>
        private bool TrySetFolded(FoldableComponent comp, bool state)
        {
            if (state == comp.IsFolded)
                return false;

            if (comp.Owner.IsInContainer())
                return false;

            // First we check if the foldable object has a strap component
            if (EntityManager.TryGetComponent(comp.OwnerUid, out StrapComponent? strap))
            {
                // If an entity is buckled to the object we can't pick it up or fold it
                if (strap.BuckledEntities.Any())
                    return false;
            }

            SetFolded(comp, state);
            return true;
        }

        /// <summary>
        /// Set the folded state of the given <see cref="FoldableComponent"/>
        /// </summary>
        /// <param name="component"></param>
        /// <param name="folded">If true, the component will become folded, else unfolded</param>
        private void SetFolded(FoldableComponent component, bool folded)
        {
            component.IsFolded = folded;
            component.CanBeFolded = !component.Owner.IsInContainer();

            // You can't buckle an entity to a folded object
            if (EntityManager.TryGetComponent(component.OwnerUid, out StrapComponent? strap))
                strap.Enabled = !component.IsFolded;

            // Update visuals only if the value has changed
            if (EntityManager.TryGetComponent(component.OwnerUid, out AppearanceComponent? appearance))
                appearance.SetData("FoldedState", folded);
        }

        #region Event handlers

        /// <summary>
        /// Handle pickup events and block them if the object is unfolded
        /// </summary>
        /// <param name="uid">Target entity</param>
        /// <param name="component">Attached foldable component</param>
        /// <param name="args"></param>
        private void OnInteract(EntityUid uid, FoldableComponent component, InteractHandEvent args)
        {
            if (args.Handled) return;

            // Try to fold, if succeeded prevent from being picked up
            if (TrySetFolded(component, true))
                args.Handled = true;

            // Else, let it be picked up
        }

        /// <summary>
        /// Prevents foldable objects to be picked up when unfolded
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="component"></param>
        /// <param name="args"></param>
        private void OnPickedUpAttempt(EntityUid uid, FoldableComponent component, AttemptItemPickupEvent args)
        {
            if (!component.IsFolded)
                args.Cancel();
        }

        #endregion

        #region Verb

        private void AddFoldVerb(EntityUid uid, FoldableComponent component, GetInteractionVerbsEvent args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            Verb verb = new()
            {
                Act = () => TryToggleFold(component),
                Text = component.IsFolded ? Loc.GetString("unfold-verb") : Loc.GetString("fold-verb"),
                IconTexture = "/Textures/Interface/VerbIcons/fold.svg.192dpi.png",

                // If the object is unfolded and they click it, they want to fold it, if it's folded, they want to pick it up
                Priority = component.IsFolded ? 0 : 2
            };

            args.Verbs.Add(verb);
        }

        #endregion
    }
}
