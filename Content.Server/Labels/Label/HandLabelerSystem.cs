using Content.Server.Labels.Components;
using Content.Server.UserInterface;
using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Labels;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Labels
{
    /// <summary>
    /// A hand labeler system that lets an object apply labels to objects with the <see cref="LabelComponent"/> .
    /// </summary>
    [UsedImplicitly]
    public sealed class HandLabelerSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly LabelSystem _labelSystem = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandLabelerComponent, AfterInteractEvent>(AfterInteractOn);
            SubscribeLocalEvent<HandLabelerComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<HandLabelerComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
            // Bound UI subscriptions
            SubscribeLocalEvent<HandLabelerComponent, HandLabelerLabelChangedMessage>(OnHandLabelerLabelChanged);
        }

        private void OnUtilityVerb(EntityUid uid, HandLabelerComponent handLabeler, GetVerbsEvent<UtilityVerb> args)
        {
            if (args.Target is not { Valid: true } target || !handLabeler.Whitelist.IsValid(target) || !args.CanAccess)
                return;

            string labelerText = handLabeler.AssignedLabel == string.Empty ? Loc.GetString("hand-labeler-remove-label-text") : Loc.GetString("hand-labeler-add-label-text");

            var verb = new UtilityVerb()
            {
                Act = () =>
                {
                    AddLabelTo(uid, handLabeler, target, out var result);
                    if (result != null)
                        _popupSystem.PopupEntity(result, args.User, args.User);
                },
                Text = labelerText
            };

            args.Verbs.Add(verb);
        }
        private void AfterInteractOn(EntityUid uid, HandLabelerComponent handLabeler, AfterInteractEvent args)
        {
            if (args.Target is not {Valid: true} target || !handLabeler.Whitelist.IsValid(target) || !args.CanReach)
                return;

            AddLabelTo(uid, handLabeler, target, out string? result);
            if (result == null)
                return;
            _popupSystem.PopupEntity(result, args.User, args.User);

            // Log labeling
            _adminLogger.Add(LogType.Action, LogImpact.Low,
                $"{ToPrettyString(args.User):user} labeled {ToPrettyString(target):target} with {ToPrettyString(uid):labeler}");
        }

        private void AddLabelTo(EntityUid uid, HandLabelerComponent? handLabeler, EntityUid target, out string? result)
        {
            if (!Resolve(uid, ref handLabeler))
            {
                result = null;
                return;
            }

            if (handLabeler.AssignedLabel == string.Empty)
            {
                _labelSystem.Label(target, null);
                result = Loc.GetString("hand-labeler-successfully-removed");
                return;
            }

            _labelSystem.Label(target, handLabeler.AssignedLabel);
            result = Loc.GetString("hand-labeler-successfully-applied");
        }

        private void OnActivate(EntityUid uid, HandLabelerComponent handLabeler, ActivateInWorldEvent args)
        {
            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;

            handLabeler.Owner.GetUIOrNull(HandLabelerUiKey.Key)?.Open(actor.PlayerSession);
            args.Handled = true;
        }

        private void OnHandLabelerLabelChanged(EntityUid uid, HandLabelerComponent handLabeler, HandLabelerLabelChangedMessage args)
        {
            if (args.Session.AttachedEntity is not {Valid: true} player)
                return;

            handLabeler.AssignedLabel = args.Label.Trim().Substring(0, Math.Min(handLabeler.MaxLabelChars, args.Label.Length));
            DirtyUI(uid, handLabeler);

            // Log label change
            _adminLogger.Add(LogType.Action, LogImpact.Low,
                $"{ToPrettyString(player):user} set {ToPrettyString(uid):labeler} to apply label \"{handLabeler.AssignedLabel}\"");

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
