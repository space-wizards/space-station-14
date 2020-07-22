using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Research;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Research;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameObjects.Components.Research
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class ResearchConsoleComponent : SharedResearchConsoleComponent, IActivate
    {

#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IRobustRandom _random;
#pragma warning restore 649

        private BoundUserInterface _userInterface;
        private ResearchClientComponent _client;
        private PowerReceiverComponent _powerReceiver;
        private const string _soundCollectionName = "keyboard";

        private bool Powered => _powerReceiver.Powered;

        public override void Initialize()
        {
            base.Initialize();
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>().GetBoundUserInterface(ResearchConsoleUiKey.Key);
            _userInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            _client = Owner.GetComponent<ResearchClientComponent>();
            _powerReceiver = Owner.GetComponent<PowerReceiverComponent>();
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage message)
        {
            if (!Owner.TryGetComponent(out TechnologyDatabaseComponent database)) return;
            if (!Powered)
                return;

            switch (message.Message)
            {
                case ConsoleUnlockTechnologyMessage msg:
                    var protoMan = IoCManager.Resolve<IPrototypeManager>();
                    if (!protoMan.TryIndex(msg.Id, out TechnologyPrototype tech)) break;
                    if(!_client.Server.CanUnlockTechnology(tech)) break;
                    if (_client.Server.UnlockTechnology(tech))
                    {
                        database.SyncWithServer();
                        database.Dirty();
                        UpdateUserInterface();
                    }

                    break;

                case ConsoleServerSyncMessage msg:
                    database.SyncWithServer();
                    UpdateUserInterface();
                    break;

                case ConsoleServerSelectionMessage msg:
                    if (!Owner.TryGetComponent(out ResearchClientComponent client)) break;
                    client.OpenUserInterface(message.Session);
                    break;
            }
        }

        /// <summary>
        ///     Method to update the user interface on the clients.
        /// </summary>
        public void UpdateUserInterface()
        {
            _userInterface.SetState(GetNewUiState());
        }

        private ResearchConsoleBoundInterfaceState GetNewUiState()
        {
            var points = _client.ConnectedToServer ? _client.Server.Point : 0;
            var pointsPerSecond = _client.ConnectedToServer ? _client.Server.PointsPerSecond : 0;

            return new ResearchConsoleBoundInterfaceState(points, pointsPerSecond);
        }

        /// <summary>
        ///     Open the user interface on a certain player session.
        /// </summary>
        /// <param name="session">Session where the UI will be shown</param>
        public void OpenUserInterface(IPlayerSession session)
        {
            _userInterface.Open(session);
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor))
                return;
            if (!Powered)
            {
                return;
            }

            OpenUserInterface(actor.playerSession);
            PlayKeyboardSound();
            return;
        }

        private void PlayKeyboardSound()
        {
            var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(_soundCollectionName);
            var file = _random.Pick(soundCollection.PickFiles);
            var audioSystem = EntitySystem.Get<AudioSystem>();
            audioSystem.PlayFromEntity(file,Owner,AudioParams.Default);
        }


    }
}
