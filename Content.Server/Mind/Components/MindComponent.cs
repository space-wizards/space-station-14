using Content.Server.GameTicking;
using Content.Server.Ghost.Components;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.MobState.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Mind.Components
{
    /// <summary>
    ///     Stores a <see cref="Server.Mind.Mind"/> on a mob.
    /// </summary>
    [RegisterComponent]
#pragma warning disable 618
    public class MindComponent : Component, IExamine
#pragma warning restore 618
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

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
        ///     Use <see cref="Mind.TransferTo(Robust.Shared.GameObjects.EntityUid)"/> instead.
        ///     If that doesn't cover it, make something to cover it.
        /// </summary>
        public void InternalEjectMind()
        {
            if (!Deleted)
                _entMan.EventBus.RaiseLocalEvent(Owner, new MindRemovedMessage());
            Mind = null;
        }

        /// <summary>
        ///     Don't call this unless you know what the hell you're doing.
        ///     Use <see cref="Mind.TransferTo(Robust.Shared.GameObjects.EntityUid)"/> instead.
        ///     If that doesn't cover it, make something to cover it.
        /// </summary>
        public void InternalAssignMind(Mind value)
        {
            Mind = value;
            _entMan.EventBus.RaiseLocalEvent(Owner, new MindAddedMessage());
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            // Let's not create ghosts if not in the middle of the round.
            if (EntitySystem.Get<GameTicker>().RunLevel != GameRunLevel.InRound)
                return;

            if (HasMind)
            {
                if (Mind?.VisitingEntity is {Valid: true} visiting)
                {
                    if (_entMan.TryGetComponent(visiting, out GhostComponent? ghost))
                    {
                        EntitySystem.Get<SharedGhostSystem>().SetCanReturnToBody(ghost, false);
                    }

                    Mind!.TransferTo(visiting);
                }
                else if (GhostOnShutdown)
                {
                    var spawnPosition = _entMan.GetComponent<TransformComponent>(Owner).Coordinates;
                    // Use a regular timer here because the entity has probably been deleted.
                    Timer.Spawn(0, () =>
                    {
                        // Async this so that we don't throw if the grid we're on is being deleted.
                        var mapMan = IoCManager.Resolve<IMapManager>();

                        var gridId = spawnPosition.GetGridId(_entMan);
                        if (gridId == GridId.Invalid || !mapMan.GridExists(gridId))
                        {
                            spawnPosition = EntitySystem.Get<GameTicker>().GetObserverSpawnPoint();
                        }

                        var ghost = _entMan.SpawnEntity("MobObserver", spawnPosition);
                        var ghostComponent = _entMan.GetComponent<GhostComponent>(ghost);
                        EntitySystem.Get<SharedGhostSystem>().SetCanReturnToBody(ghostComponent, false);

                        if (Mind != null)
                        {
                            string? val = Mind.CharacterName ?? string.Empty;
                            _entMan.GetComponent<MetaDataComponent>(ghost).EntityName = val;
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
                _entMan.TryGetComponent<MobStateComponent?>(Owner, out var state) &&
                state.IsDead();

            if (dead)
            {
                if (Mind?.Session == null) {
                    // Player has no session attached and dead
                    message.AddMarkup($"[color=yellow]{Loc.GetString("mind-component-no-mind-and-dead-text", ("ent", Owner))}[/color]");
                } else {
                    // Player is dead with session
                    message.AddMarkup($"[color=red]{Loc.GetString("comp-mind-examined-dead", ("ent", Owner))}[/color]");
                }
            }
            else if (!HasMind)
            {
                message.AddMarkup($"[color=mediumpurple]{Loc.GetString("comp-mind-examined-catatonic", ("ent", Owner))}[/color]");
            }
            else if (Mind?.Session == null)
            {
                message.AddMarkup($"[color=yellow]{Loc.GetString("comp-mind-examined-ssd", ("ent", Owner))}[/color]");
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
