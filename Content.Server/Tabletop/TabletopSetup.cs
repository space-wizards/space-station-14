using System.Numerics;
using Robust.Shared.Prototypes;

namespace Content.Server.Tabletop;

[ImplicitDataDefinitionForInheritors]
public abstract partial class TabletopSetup
{
    /// <summary>
    /// Public wrapper for <see cref="SetupTabletop(Spawner)"/>.
    /// </summary>
    public void SetupTabletop(TabletopSession session, IEntityManager entityManager)
    {
        SetupTabletop(new Spawner(session, entityManager));
    }

    /// <summary>
    /// The implementation of a tabletop's setup. This should spawn a board and all of the pieces needed to play the game.
    /// </summary>
    /// <param name="spawner">A <see cref="Spawner"/> used to spawn entities in this tabletop during setup.</param>
    protected abstract void SetupTabletop(Spawner spawner);

    protected sealed class Spawner(TabletopSession session, IEntityManager entityManager)
    {
        /// <summary>
        /// The underlying <see cref="IEntityManager"/> which this object uses for spawning. This is provided in the case
        /// that tabletop setup needs to modify the components of spawned entities.
        /// </summary>
        public IEntityManager EntityManager => entityManager;

        /// <summary>
        /// Stores a "baked in" offset for this spawner, as set by <see cref="WithRelativeSpawnPosition"/>.
        /// </summary>
        private Vector2 _relativeSpawnPosition = Vector2.Zero;

        public EntityUid Spawn(EntProtoId proto, float x, float y)
        {
            var entity =
                entityManager.Spawn(proto, session.Position.Offset(new Vector2(x, y) + _relativeSpawnPosition));
            session.Entities.Add(entity);
            return entity;
        }

        /// <summary>
        /// Returns a decorated version of this spawner which implicitly includes <paramref name="x"/> and
        /// <paramref name="y"/> as offsets to the positions of entities spawned.
        /// </summary>
        public Spawner WithRelativeSpawnPosition(float x, float y)
        {
            return new Spawner(session, EntityManager)
            {
                _relativeSpawnPosition = _relativeSpawnPosition + new Vector2(x, y)
            };
        }
    }
}
