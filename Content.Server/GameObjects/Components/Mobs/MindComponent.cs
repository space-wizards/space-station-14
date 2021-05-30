#nullable enable
using Content.Server.GameObjects.Components.Observer;
using Content.Server.GameTicking;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Mobs;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
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
        [DataField("showExamineInfo")]
        public bool ShowExamineInfo { get; set; }

        /// <summary>
        ///     Whether the mind will be put on a ghost after this component is shutdown.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("ghostOnShutdown")]
        public bool GhostOnShutdown { get; set; } = true;

        /// <summary>
        ///     Don't call this unless you know what the hell you're doing.
        ///     Use <see cref="Mind.TransferTo(IEntity)"/> instead.
        ///     If that doesn't cover it, make something to cover it.
        /// </summary>
        public void InternalEjectMind()
        {
            if (!Deleted)
                Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new MindRemovedMessage());
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
            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new MindAddedMessage());
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            // Let's not create ghosts if not in the middle of the round.
            if (IoCManager.Resolve<IGameTicker>().RunLevel != GameRunLevel.InRound)
                return;

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
                else if (GhostOnShutdown)
                {
                    var spawnPosition = Owner.Transform.Coordinates;
                    // Use a regular timer here because the entity has probably been deleted.
                    Timer.Spawn(0, () =>
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
                            ghost.Name = Mind.CharacterName ?? string.Empty;
                            Mind.TransferTo(ghost);
                        }
                    });
                }
            }
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

    public class MindRemovedMessage : EntityEventArgs
    {
    }

    public class MindAddedMessage : EntityEventArgs
    {
    }
}
