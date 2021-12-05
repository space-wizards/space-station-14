using System;
using Content.Server.Labels.Components;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Labels;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Players;

namespace Content.Server.Labels
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

        private void AddLabelTo(EntityUid uid, HandLabelerComponent? handLabeler, EntityUid target, out string? result)
        {
            if (!Resolve(uid, ref handLabeler))
            {
                result = null;
                return;
            }

            LabelComponent label = target.EnsureComponent<LabelComponent>();

            if (label.OriginalName != null)
                IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(target).EntityName = label.OriginalName;
            label.OriginalName = null;

            if (handLabeler.AssignedLabel == string.Empty)
            {
                label.CurrentLabel = null;
                result = Loc.GetString("hand-labeler-successfully-removed");
                return;
            }

            label.OriginalName = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(target).EntityName;
            string val = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(target).EntityName + $" ({handLabeler.AssignedLabel})";
            IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(target).EntityName = val;
            label.CurrentLabel = handLabeler.AssignedLabel;
            result = Loc.GetString("hand-labeler-successfully-applied");
        }

        private void OnUseInHand(EntityUid uid, HandLabelerComponent handLabeler, UseInHandEvent args)
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(args.User, out ActorComponent? actor))
                return;

            handLabeler.Owner.GetUIOrNull(HandLabelerUiKey.Key)?.Open(actor.PlayerSession);
            args.Handled = true;
        }

        private bool CheckInteract(ICommonSession session)
        {
            if (session.AttachedEntity is not { } uid
                || !Get<ActionBlockerSystem>().CanInteract(uid)
                || !Get<ActionBlockerSystem>().CanUse(uid))
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
