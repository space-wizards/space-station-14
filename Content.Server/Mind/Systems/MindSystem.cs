using Content.Server.GameTicking;
using Content.Server.Ghost.Components;
using Content.Server.Mind.Components;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.MobState.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Mind.Systems
{
    public class MindSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _map = default!;
        [Dependency] private readonly GameTicker _ticker = default!;
        [Dependency] private readonly SharedGhostSystem _ghosts = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MindComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<MindComponent, RemovedComponentEventArgs>(OnRemove);
        }

        private void OnExamine(EntityUid uid, MindComponent component, ExaminedEvent args)
        {
            if (!component.ShowExamineInfo || !args.IsInDetailsRange)
                return;

            EntityManager.TryGetComponent(uid, out MobStateComponent? state);
            var dead = state != null &&state.IsDead();

            if (component.Mind == null)
            {
                var aliveText =
                    $"[color=purple]{Loc.GetString("comp-mind-examined-catatonic", ("ent", component.Owner))}[/color]";
                var deadText = $"[color=red]{Loc.GetString("comp-mind-examined-dead", ("ent", component.Owner))}[/color]";

                args.PushMarkup(dead ? deadText : aliveText);
            }
            else if (component.Mind?.Session == null)
            {
                if (dead) return;

                var text =
                    $"[color=yellow]{Loc.GetString("comp-mind-examined-ssd", ("ent", component.Owner))}[/color]";

                args.PushMarkup(text);
            }
        }

        private void OnRemove(EntityUid uid, MindComponent component, RemovedComponentEventArgs args)
        {
            // Let's not create ghosts if not in the middle of the round.
            if (_ticker.RunLevel != GameRunLevel.InRound)
                return;
            if (component.Mind == null)
                return;

            var visiting = component.Mind.VisitingEntity;
            if (visiting != null)
            {
                if (visiting.TryGetComponent(out GhostComponent? ghost))
                {
                    _ghosts.SetCanReturnToBody(ghost, false);
                }
                component.Mind.TransferTo(visiting);
            }
            else if (component.GhostOnShutdown)
            {
                if (!EntityManager.TryGetComponent(uid, out TransformComponent transform))
                    return;
                var spawnPosition = transform.Coordinates;

                // Use a regular timer here because the entity has probably been deleted.
                // wth is this magic?
                Timer.Spawn(0, () =>
                {
                    // Async this so that we don't throw if the grid we're on is being deleted.
                    var gridId = spawnPosition.GetGridId(EntityManager);
                    if (gridId == GridId.Invalid || !_map.GridExists(gridId))
                    {
                        spawnPosition = _ticker.GetObserverSpawnPoint();
                    }

                    var ghost = EntityManager.SpawnEntity("MobObserver", spawnPosition);
                    var ghostComponent = ghost.GetComponent<GhostComponent>();
                    _ghosts.SetCanReturnToBody(ghostComponent, false);

                    if (component.Mind != null)
                    {
                        ghost.Name = component.Mind.CharacterName ?? string.Empty;
                        component.Mind.TransferTo(ghost);
                    }
                });
            }
        }

        /// <summary>
        ///     Don't call this unless you know what the hell you're doing.
        ///     Use <see cref="Mind.TransferTo"/> instead.
        ///     If that doesn't cover it, make something to cover it.
        /// </summary>
        public void InternalAssignMind(EntityUid uid, Mind value, MindComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.Mind = value;
            RaiseLocalEvent(uid, new MindAddedMessage());
        }

        /// <summary>
        ///     Don't call this unless you know what the hell you're doing.
        ///     Use <see cref="Mind.TransferTo"/> instead.
        ///     If that doesn't cover it, make something to cover it.
        /// </summary>
        public void InternalEjectMind(EntityUid uid, MindComponent? component = null)
        {
            // we don't log failed resolve, because entity
            // could be already deleted. It's ok
            if (!Resolve(uid, ref component, logMissing: false))
                return;
            if (!EntityManager.EntityExists(uid))
                return;

            RaiseLocalEvent(uid, new MindRemovedMessage());
            component.Mind = null;
        }
    }
}
