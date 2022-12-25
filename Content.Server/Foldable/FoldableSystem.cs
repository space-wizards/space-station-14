using System.Linq;
using Content.Server.Buckle.Systems;
using Content.Server.Storage.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Foldable;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Containers;

namespace Content.Server.Foldable
{
    [UsedImplicitly]
    public sealed class FoldableSystem : SharedFoldableSystem
    {
        [Dependency] private BuckleSystem _buckle = default!;
        [Dependency] private SharedContainerSystem _container = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FoldableComponent, StorageOpenAttemptEvent>(OnFoldableOpenAttempt);
            SubscribeLocalEvent<FoldableComponent, GetVerbsEvent<AlternativeVerb>>(AddFoldVerb);
            SubscribeLocalEvent<FoldableComponent, StoreMobInItemContainerAttemptEvent>(OnStoreThisAttempt);

        }

        private void OnFoldableOpenAttempt(EntityUid uid, FoldableComponent component, StorageOpenAttemptEvent args)
        {
            if (component.IsFolded)
                args.Cancel();
        }

        public bool TryToggleFold(FoldableComponent comp)
        {
            return TrySetFolded(comp, !comp.IsFolded);
        }

        public bool CanToggleFold(EntityUid uid, FoldableComponent? fold = null)
        {
            if (!Resolve(uid, ref fold))
                return false;

            // Can't un-fold in any container (locker, hands, inventory, whatever).
            if (_container.IsEntityInContainer(uid))
                return false;

            // If an entity is buckled to the object we can't pick it up or fold it
            if (TryComp(uid, out StrapComponent? strap) && strap.BuckledEntities.Any())
                return false;

            if (!TryComp(uid, out EntityStorageComponent? storage))
                return true;

            if (storage.Open)
                return false;

            return !storage.Contents.ContainedEntities.Any();
        }

        /// <summary>
        /// Try to fold/unfold
        /// </summary>
        /// <param name="comp"></param>
        /// <param name="state">Folded state we want</param>
        /// <returns>True if successful</returns>
        public bool TrySetFolded(FoldableComponent comp, bool state)
        {
            if (state == comp.IsFolded)
                return false;

            if (!CanToggleFold(comp.Owner, comp))
                return false;

            SetFolded(comp, state);
            return true;
        }

        /// <summary>
        /// Set the folded state of the given <see cref="FoldableComponent"/>
        /// </summary>
        /// <param name="component"></param>
        /// <param name="folded">If true, the component will become folded, else unfolded</param>
        public override void SetFolded(FoldableComponent component, bool folded)
        {
            base.SetFolded(component, folded);

            // You can't buckle an entity to a folded object
            _buckle.StrapSetEnabled(component.Owner, !component.IsFolded);
        }

        public void OnStoreThisAttempt(EntityUid uid, FoldableComponent comp, StoreMobInItemContainerAttemptEvent args)
        {
            args.Handled = true;

            if (comp.IsFolded)
                args.Cancel();
        }

        #region Verb

        private void AddFoldVerb(EntityUid uid, FoldableComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract || args.Hands == null || !CanToggleFold(uid, component))
                return;

            AlternativeVerb verb = new()
            {
                Act = () => TryToggleFold(component),
                Text = component.IsFolded ? Loc.GetString("unfold-verb") : Loc.GetString("fold-verb"),
                IconTexture = "/Textures/Interface/VerbIcons/fold.svg.192dpi.png",

                // If the object is unfolded and they click it, they want to fold it, if it's folded, they want to pick it up
                Priority = component.IsFolded ? 0 : 2,
            };

            args.Verbs.Add(verb);
        }

        #endregion
    }
}
