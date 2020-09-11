#nullable enable
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.Utility;
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
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Research
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class ResearchConsoleComponent : SharedResearchConsoleComponent, IActivate
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        private const string SoundCollectionName = "keyboard";

        [ViewVariables] private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(ResearchConsoleUiKey.Key);

        public override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }

            Owner.EnsureComponent<ResearchClientComponent>();
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage message)
        {
            if (!Owner.TryGetComponent(out TechnologyDatabaseComponent? database))
                return;
            if (!Owner.TryGetComponent(out ResearchClientComponent? client))
                return;
            if (!Powered)
                return;

            switch (message.Message)
            {
                case ConsoleUnlockTechnologyMessage msg:
                    if (!_prototypeManager.TryIndex(msg.Id, out TechnologyPrototype tech)) break;
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
            if (!Owner.TryGetComponent(out ResearchClientComponent? client) ||
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
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor))
                return;
            if (!Powered)
            {
                return;
            }

            OpenUserInterface(actor.playerSession);
            PlayKeyboardSound();
        }

        private void PlayKeyboardSound()
        {
            var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(SoundCollectionName);
            var file = _random.Pick(soundCollection.PickFiles);
            var audioSystem = EntitySystem.Get<AudioSystem>();
            audioSystem.PlayFromEntity(file,Owner,AudioParams.Default);
        }
    }
}
