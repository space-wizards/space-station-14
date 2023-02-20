using Content.Server.Labels.Components;
using Content.Server.UserInterface;
using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.NukeOps;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Content.Server.GameTicking.Rules;

namespace Content.Server.NukeOps
{
    /// <summary>
    /// War declarator that used in NukeOps game rule for declaring war
    /// </summary>
    [UsedImplicitly]
    public sealed class WarDeclaratorSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly NukeopsRuleSystem _nukeopsRuleSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<WarDeclaratorComponent, ActivateInWorldEvent>(OnActivate);
            // Bound UI subscriptions
            SubscribeLocalEvent<WarDeclaratorComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<WarDeclaratorComponent, WarDeclaratorChangedMessage>(OnMessageChanged);
            SubscribeLocalEvent<WarDeclaratorComponent, WarDeclaratorPressedWarButton>(OnWarButtonPressed);
        }

        private void OnComponentInit(EntityUid uid, WarDeclaratorComponent warDeclarator, ComponentInit args)
        {
            DirtyUI(uid, warDeclarator);
        }

        private void OnActivate(EntityUid uid, WarDeclaratorComponent handLabeler, ActivateInWorldEvent args)
        {
            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;

            handLabeler.Owner.GetUIOrNull(WarDeclaratorUiKey.Key)?.Open(actor.PlayerSession);
            args.Handled = true;
        }

        private void OnMessageChanged(EntityUid uid, WarDeclaratorComponent warDeclarator, WarDeclaratorChangedMessage args)
        {
            if (args.Session.AttachedEntity is not {Valid: true} player)
                return;

            warDeclarator.Message = args.Message.Trim().Substring(0, Math.Min(warDeclarator.MaxMessageLength, args.Message.Length));
            DirtyUI(uid, warDeclarator);
        }

        private void OnWarButtonPressed(EntityUid uid, WarDeclaratorComponent warDeclarator, WarDeclaratorPressedWarButton args)
        {
            if (args.Session.AttachedEntity is not {Valid: true} player)
                return;

            if (_nukeopsRuleSystem.GetWarCondition() != WarConditionStatus.YES_WAR)
            {
                DirtyUI(uid, warDeclarator);
                return;
            }

            warDeclarator.Message = String.Empty;
            var msg = "Declarator seems to trying declare war but it can't"; //Loc.GetString("");
            _popupSystem.PopupEntity(msg, uid);
            RefreshAllDeclaratorsUI();
        }

        private void RefreshAllDeclaratorsUI()
        {
            foreach (var comp in EntityQuery<WarDeclaratorComponent>())
            {
                DirtyUI(comp.Owner, comp);
            }
        }

        private void DirtyUI(EntityUid uid,
            WarDeclaratorComponent? warDeclarator = null)
        {
            if (!Resolve(uid, ref warDeclarator))
                return;

            _userInterfaceSystem.TrySetUiState(uid, WarDeclaratorUiKey.Key,
                new WarDeclaratorBoundUserInterfaceState
                (
                    warDeclarator.Message,
                    _nukeopsRuleSystem.GetWarCondition(),
                    _nukeopsRuleSystem.NukeopsRuleConfig.WarMinCrewSize,
                    _nukeopsRuleSystem.NukeopsRuleConfig.WarTimeLimit
                )
            );
        }
    }
}
