using Content.Server.HandLabeler.Components;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.HandLabeler;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.IoC;
using JetBrains.Annotations;
using Robust.Shared.Localization;
using Content.Shared.Popups;
using System;

namespace Content.Server.HandLabeler
{
    /// <summary>
    /// A hand labeler system that lets an object apply labels to objects with the <see cref="LabelComponent"/> .
    /// </summary>
    [UsedImplicitly]
    public class HandLabelerSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandLabelerComponent, AfterInteractEvent>(AfterInteractOn);
            SubscribeLocalEvent<HandLabelerComponent, UseInHandEvent>(OnUseInHand);
            // Bound UI subscriptions
            SubscribeLocalEvent<HandLabelerComponent, HandLabelerLabelChangedMessage>(OnHandLabelerLabelChanged);
        }

        private void AfterInteractOn(EntityUid uid, HandLabelerComponent handLabeler, AfterInteractEvent args)
        {
            if (args.Target == null || !handLabeler.Whitelist.IsValid(args.Target))
                return;

            AddLabelTo(uid, handLabeler, args.Target, out string? result);
            if (result != null)
                handLabeler.Owner.PopupMessage(args.User, result);
        }

        private void AddLabelTo(EntityUid uid, HandLabelerComponent? handLabeler, IEntity target, out string? result)
        {
            if (!Resolve(uid, ref handLabeler))
            {
                result = null;
                return;
            }

            LabelComponent label = target.EnsureComponent<LabelComponent>();

            if (label.OriginalName != null)
                target.Name = label.OriginalName;
            label.OriginalName = null;

            if (handLabeler.AssignedLabel == string.Empty)
            {
                label.CurrentLabel = null;
                result = Loc.GetString("hand-labeler-successfully-removed");
                return;
            }

            label.OriginalName = target.Name;
            target.Name += $" ({handLabeler.AssignedLabel})";
            label.CurrentLabel = handLabeler.AssignedLabel;
            result = Loc.GetString("hand-labeler-successfully-applied");
        }

        private void OnUseInHand(EntityUid uid, HandLabelerComponent handLabeler, UseInHandEvent args)
        {
            if (!args.User.TryGetComponent(out ActorComponent? actor))
                return;

            handLabeler.Owner.GetUIOrNull(HandLabelerUiKey.Key)?.Open(actor.PlayerSession);
            args.Handled = true;
        }

        private bool CheckInteract(ICommonSession session)
        {
            if (session.AttachedEntity is not { } entity
                || !Get<ActionBlockerSystem>().CanInteract(entity)
                || !Get<ActionBlockerSystem>().CanUse(entity))
                return false;

            return true;
        }

        private void OnHandLabelerLabelChanged(EntityUid uid, HandLabelerComponent handLabeler, HandLabelerLabelChangedMessage args)
        {
            if (!CheckInteract(args.Session))
                return;

            handLabeler.AssignedLabel = args.Label.Trim().Substring(0, Math.Min(handLabeler.MaxLabelChars, args.Label.Length));
            DirtyUI(uid, handLabeler);
        }

        private void DirtyUI(EntityUid uid,
            HandLabelerComponent? handLabeler = null)
        {
            if (!Resolve(uid, ref handLabeler))
                return;

            _userInterfaceSystem.TrySetUiState(uid, HandLabelerUiKey.Key,
                new HandLabelerBoundUserInterfaceState(handLabeler.AssignedLabel));
        }
    }
}
