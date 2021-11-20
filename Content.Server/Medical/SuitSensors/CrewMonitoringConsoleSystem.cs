using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Medical.SuitSensors;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Medical.SuitSensors
{
    public class CrewMonitoringConsoleSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CrewMonitoringConsoleComponent, ActivateInWorldEvent>(OnActivate);
        }

        private void OnActivate(EntityUid uid, CrewMonitoringConsoleComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;

            // standard interactions check
            if (!args.InRangeUnobstructed())
                return;
            if (!_actionBlocker.CanInteract(args.User.Uid) || !_actionBlocker.CanUse(args.User.Uid))
                return;

            if (!EntityManager.TryGetComponent(args.User.Uid, out ActorComponent? actor))
                return;

            ShowUI(uid, actor.PlayerSession, component);
            args.Handled = true;
        }

        private void ShowUI(EntityUid uid, IPlayerSession session, CrewMonitoringConsoleComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            var ui = component.Owner.GetUIOrNull(CrewMonitoringUIKey.Key);
            ui?.Open(session);

            //UpdateUserInterface(uid, component);
        }
    }
}
