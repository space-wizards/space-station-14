using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Hands.Components;
using Content.Server.UserInterface;
using Content.Shared.Atmos;
using Content.Shared.Medical.Components;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;
using Content.Shared.Damage;
using Content.Shared.Body.Components;
using Content.Shared.MobState;

namespace Content.Server.Medical.Components
{
    [RegisterComponent]
    public class HealthAnalyzerComponent : SharedHealthAnalyzerComponent, IAfterInteract, IDropped, IUse
    {
        private IEntity? _target; // Scan target
        private AppearanceComponent? _appearance;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(HealthAnalyzerUiKey.Key);

        protected override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
                UserInterface.OnClosed += UserInterfaceOnClose;
            }

            Owner.TryGetComponent(out _appearance);
        }

        public void OpenInterface(IPlayerSession session, IEntity? target)
        {
            _target = target;
            UserInterface?.Open(session);
            UpdateUserInterface();
            UpdateAppearance(true);
            Resync();
        }

        public void ToggleInterface(IPlayerSession session)
        {
            if (UserInterface == null)
                return;

            if (UserInterface.SessionHasOpen(session))
                CloseInterface(session);
            else
                OpenInterface(session, null);
        }

        public void CloseInterface(IPlayerSession session)
        {
            _target = null;
            UserInterface?.Close(session);
            // Our OnClose will do the appearance stuff
            Resync();
        }

        private void UserInterfaceOnClose(IPlayerSession obj)
        {
            UpdateAppearance(false);
        }

        private void UpdateAppearance(bool open)
        {
            _appearance?.SetData(HealthAnalyzerVisuals.VisualState,
                open ? HealthAnalyzerVisualState.Working : HealthAnalyzerVisualState.Off);
        }

        private void Resync()
        {
            Dirty();
        }

        private void UpdateUserInterface()
        {
            if (UserInterface == null)
            {
                return;
            }

            string? error = null;

            // Check if the player is still holding the gas analyzer => if not, don't update
            foreach (var session in UserInterface.SubscribedSessions)
            {
                if (session.AttachedEntity == null)
                    return;

                if (!session.AttachedEntity.TryGetComponent(out IHandsComponent? handsComponent))
                    return;

                var activeHandEntity = handsComponent?.GetActiveHand?.Owner;
                if (activeHandEntity == null || !activeHandEntity.TryGetComponent(out HealthAnalyzerComponent? healthAnalyzer))
                {
                    return;
                }
            }

            var pos = Owner.Transform.Coordinates;
            if (_target == null)
            {
                error = "No organic matter detected";
                UserInterface.SetState(
                new HealthAnalyzerBoundUserInterfaceState(
                    0,
                    error));
                return;
            }

            if (_target.TryGetComponent(out IMobStateComponent? mobState) &&
                _target.TryGetComponent(out DamageableComponent? damageable) &&
                _target.TryGetComponent(out SharedBodyComponent? body)
            ){

                /*foreach (var (part, _) in body.Parts){
                    part.
                }*/
                int threshold;
                if (!mobState.TryGetEarliestDeadState(damageable.TotalDamage, out _, out threshold)){
                    error = "No vital signs detected";
                    UserInterface.SetState(
                    new HealthAnalyzerBoundUserInterfaceState(
                        0,
                        error));
                    return;
                }
                var health = (1 - (float) damageable.TotalDamage / threshold) * 100;

                //get health data and organs of the entity
                UserInterface.SetState(
                    new HealthAnalyzerBoundUserInterfaceState(
                        health,
                        error));
            } else {
                error = "No vital signs detected";
                UserInterface.SetState(
                new HealthAnalyzerBoundUserInterfaceState(
                    0,
                    error));
                return;
            }


        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            var message = serverMsg.Message;
            switch (message)
            {
                case HealthAnalyzerRefreshMessage msg:
                    var player = serverMsg.Session.AttachedEntity;
                    if (player == null)
                    {
                        return;
                    }

                    if (!player.TryGetComponent(out IHandsComponent? handsComponent))
                    {
                        Owner.PopupMessage(player, Loc.GetString("health-analyzer-component-player-has-no-hands-message"));
                        return;
                    }

                    var activeHandEntity = handsComponent.GetActiveHand?.Owner;
                    if (activeHandEntity == null || !activeHandEntity.TryGetComponent(out HealthAnalyzerComponent? healthAnalyzer))
                    {
                        serverMsg.Session.AttachedEntity?.PopupMessage(Loc.GetString("health-analyzer-component-need-gas-analyzer-in-hand-message"));
                        return;
                    }

                    UpdateUserInterface();
                    Resync();
                    break;
            }
        }

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!eventArgs.CanReach)
            {
                eventArgs.User.PopupMessage(Loc.GetString("health-analyzer-component-player-cannot-reach-message"));
                return true;
            }

            if (eventArgs.User.TryGetComponent(out ActorComponent? actor))
            {
                OpenInterface(actor.PlayerSession, eventArgs.Target);
            }

            return true;
        }



        void IDropped.Dropped(DroppedEventArgs eventArgs)
        {
            if (eventArgs.User.TryGetComponent(out ActorComponent? actor))
            {
                CloseInterface(actor.PlayerSession);
            }
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            if (eventArgs.User.TryGetComponent(out ActorComponent? actor))
            {
                ToggleInterface(actor.PlayerSession);
                return true;
            }
            return false;
        }
    }
}
