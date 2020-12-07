#nullable enable
using Content.Server.GameObjects.Components.Medical;
using Content.Server.GameObjects.Components.Observer;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Mobs;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Timers;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Mobs
{
    /// <summary>
    ///     Stores a <see cref="Server.Mobs.Mind"/> on a mob.
    /// </summary>
    [RegisterComponent]
    public class MindComponent : Component, IExamine
    {
        private bool _showExamineInfo;

        /// <inheritdoc />
        public override string Name => "Mind";

        /// <summary>
        ///     The mind controlling this mob. Can be null.
        /// </summary>
        [ViewVariables]
        public Mind? Mind { get; private set; }

        /// <summary>
        ///     True if we have a mind, false otherwise.
        /// </summary>
        [ViewVariables]
        public bool HasMind => Mind != null;

        /// <summary>
        ///     Whether examining should show information about the mind or not.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ShowExamineInfo
        {
            get => _showExamineInfo;
            set => _showExamineInfo = value;
        }

        [ViewVariables]
        private BoundUserInterface? UserInterface =>
            Owner.GetUIOrNull(SharedAcceptCloningComponent.AcceptCloningUiKey.Key);


        public override void Initialize()
        {
            base.Initialize();
            Owner.EntityManager.EventBus.SubscribeEvent<CloningPodComponent.CloningStartedMessage>(
                EventSource.Local, this,
                HandleCloningStartedMessage);

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiAcceptCloningMessage;
            }
        }

        private void HandleCloningStartedMessage(CloningPodComponent.CloningStartedMessage ev)
        {
            if (ev.CapturedMind == Mind)
            {
                UserInterface?.Open(Mind.Session);
            }
        }

        private void OnUiAcceptCloningMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Message is not SharedAcceptCloningComponent.UiButtonPressedMessage) return;
            if (Mind != null)
            {
                Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new GhostComponent.GhostReturnMessage(Mind));
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();
            Owner.EntityManager.EventBus.UnsubscribeEvent<CloningPodComponent.CloningStartedMessage>(EventSource.Local, this);
            if (UserInterface != null) UserInterface.OnReceiveMessage -= OnUiAcceptCloningMessage;
        }

        /// <summary>
        ///     Don't call this unless you know what the hell you're doing.
        ///     Use <see cref="Mind.TransferTo(IEntity)"/> instead.
        ///     If that doesn't cover it, make something to cover it.
        /// </summary>
        public void InternalEjectMind()
        {
            Mind = null;
        }

        /// <summary>
        ///     Don't call this unless you know what the hell you're doing.
        ///     Use <see cref="Mind.TransferTo(IEntity)"/> instead.
        ///     If that doesn't cover it, make something to cover it.
        /// </summary>
        public void InternalAssignMind(Mind value)
        {
            Mind = value;
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            if (HasMind)
            {
                var visiting = Mind?.VisitingEntity;
                if (visiting != null)
                {
                    if (visiting.TryGetComponent(out GhostComponent? ghost))
                    {
                        ghost.CanReturnToBody = false;
                    }

                    Mind!.TransferTo(visiting);
                }
                else
                {
                    var spawnPosition = Owner.Transform.Coordinates;
                    Owner.SpawnTimer(0, () =>
                    {
                        // Async this so that we don't throw if the grid we're on is being deleted.
                        var mapMan = IoCManager.Resolve<IMapManager>();

                        var gridId = spawnPosition.GetGridId(Owner.EntityManager);
                        if (gridId == GridId.Invalid || !mapMan.GridExists(gridId))
                        {
                            spawnPosition = IoCManager.Resolve<IGameTicker>().GetObserverSpawnPoint();
                        }

                        var ghost = Owner.EntityManager.SpawnEntity("MobObserver", spawnPosition);
                        var ghostComponent = ghost.GetComponent<GhostComponent>();
                        ghostComponent.CanReturnToBody = false;

                        if (Mind != null)
                        {
                            ghost.Name = Mind.CharacterName;
                            Mind.TransferTo(ghost);
                        }
                    });
                }
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _showExamineInfo, "show_examine_info", false);
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!ShowExamineInfo || !inDetailsRange)
            {
                return;
            }

            var dead =
                Owner.TryGetComponent<IMobStateComponent>(out var state) &&
                state.IsDead();

            if (!HasMind)
            {
                var aliveText =
                    $"[color=red]{Loc.GetString("{0:They} {0:are} totally catatonic. The stresses of life in deep-space must have been too much for {0:them}. Any recovery is unlikely.", Owner)}[/color]";
                var deadText = $"[color=purple]{Loc.GetString("{0:Their} soul has departed.", Owner)}[/color]";

                message.AddMarkup(dead ? deadText : aliveText);
            }
            else if (Mind?.Session == null)
            {
                if (dead) return;

                var text =
                    $"[color=yellow]{Loc.GetString("{0:They} {0:have} a blank, absent-minded stare and appears completely unresponsive to anything. {0:They} may snap out of it soon.", Owner)}[/color]";

                message.AddMarkup(text);
            }
        }
    }
}
