using System.Linq;
using Content.Server.Storage.Components;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Foldable;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server.Foldable
{
    [UsedImplicitly]
    public sealed class FoldableSystem : SharedFoldableSystem
    {
        [Dependency] private readonly SharedContainerSystem _container = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FoldableComponent, GetVerbsEvent<AlternativeVerb>>(AddFoldVerb);

        }
        public bool TryToggleFold(EntityUid uid, FoldableComponent comp)
        {
            return TrySetFolded(uid, comp, !comp.IsFolded);
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
        public bool TrySetFolded(EntityUid uid, FoldableComponent comp, bool state)
        {
            if (state == comp.IsFolded)
                return false;

            if (!CanToggleFold(uid, comp))
                return false;

            SetFolded(uid, comp, state);
            return true;
        }

        #region Verb

        private void AddFoldVerb(EntityUid uid, FoldableComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract || args.Hands == null || !CanToggleFold(uid, component))
                return;

            AlternativeVerb verb = new()
            {
                Act = () => TryToggleFold(uid, component),
                Text = component.IsFolded ? Loc.GetString("unfold-verb") : Loc.GetString("fold-verb"),
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/fold.svg.192dpi.png")),

                // If the object is unfolded and they click it, they want to fold it, if it's folded, they want to pick it up
                Priority = component.IsFolded ? 0 : 2,
            };

            args.Verbs.Add(verb);
        }

        #endregion
    }
}
