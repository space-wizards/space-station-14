using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.Sound;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Research.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class ResearchConsoleComponent : SharedResearchConsoleComponent, IActivate
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [DataField("sound")]
        private SoundSpecifier _soundCollectionName = new SoundCollectionSpecifier("keyboard");

        [ViewVariables] private bool Powered => !_entMan.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver) || receiver.Powered;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(ResearchConsoleUiKey.Key);

        protected override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponentWarn<ServerUserInterfaceComponent>();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }

            Owner.EnsureComponent<ResearchClientComponent>();
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage message)
        {
            if (!_entMan.TryGetComponent(Owner, out TechnologyDatabaseComponent? database))
                return;
            if (!_entMan.TryGetComponent(Owner, out ResearchClientComponent? client))
                return;
            if (!Powered)
                return;

            switch (message.Message)
            {
                case ConsoleUnlockTechnologyMessage msg:
                    if (!_prototypeManager.TryIndex(msg.Id, out TechnologyPrototype? tech)) break;
                    if (client.Server == null) break;
                    if (!client.Server.CanUnlockTechnology(tech)) break;
                    if (client.Server.UnlockTechnology(tech))
                    {
                        database.SyncWithServer();
                        database.Dirty();
                        UpdateUserInterface();
                    }

                    break;

                case ConsoleServerSyncMessage _:
                    database.SyncWithServer();
                    UpdateUserInterface();
                    break;

                case ConsoleServerSelectionMessage _:
                    client.OpenUserInterface(message.Session);
                    break;
            }
        }

        /// <summary>
        ///     Method to update the user interface on the clients.
        /// </summary>
        public void UpdateUserInterface()
        {
            UserInterface?.SetState(GetNewUiState());
        }

        private ResearchConsoleBoundInterfaceState GetNewUiState()
        {
            if (!_entMan.TryGetComponent(Owner, out ResearchClientComponent? client) ||
                client.Server == null)
                return new ResearchConsoleBoundInterfaceState(default, default);

            var points = client.ConnectedToServer ? client.Server.Point : 0;
            var pointsPerSecond = client.ConnectedToServer ? client.Server.PointsPerSecond : 0;

            return new ResearchConsoleBoundInterfaceState(points, pointsPerSecond);
        }

        /// <summary>
        ///     Open the user interface on a certain player session.
        /// </summary>
        /// <param name="session">Session where the UI will be shown</param>
        public void OpenUserInterface(IPlayerSession session)
        {
            UserInterface?.Open(session);
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!_entMan.TryGetComponent(eventArgs.User, out ActorComponent? actor))
                return;
            if (!Powered)
            {
                return;
            }

            OpenUserInterface(actor.PlayerSession);
            PlayKeyboardSound();
        }

        private void PlayKeyboardSound()
        {
            SoundSystem.Play(Filter.Pvs(Owner), _soundCollectionName.GetSound(), Owner, AudioParams.Default);
        }
    }
}
