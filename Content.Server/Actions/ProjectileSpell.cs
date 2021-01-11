using Content.Server.Utility;
using Content.Shared.Actions;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Server.Actions
{
    [UsedImplicitly]
    public class ProjectileSpell : ITargetPointAction
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public string CastMessage { get; private set; }
        public string Projectile { get; private set; }

        public bool Stationary { get; private set; }

        public ProjectileSpell()
        {
            IoCManager.InjectDependencies(this);
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.CastMessage, "castmessage", "Instant action used.");
            serializer.DataField(this, x => x.Projectile, "spellprojectile", null);
            serializer.DataField(this, x => x.Stationary, "trap", false);
        }

        public void DoTargetPointAction(TargetPointActionEventArgs args)
        {
            var playerPosition = args.Performer.Transform.LocalPosition;
            var direction = (args.Target.Position - playerPosition).Normalized * 2;
            var coords = args.Performer.Transform.Coordinates.WithPosition(playerPosition + direction);

            args.Performer.PopupMessageEveryone(CastMessage);

            var spawnedSpell = _entityManager.SpawnEntity(Projectile, coords);

            if (Stationary == true)
            {
                return;
            }

            var _physicsComponent = spawnedSpell.GetComponent<PhysicsComponent>();
            _physicsComponent.ApplyImpulse(direction * 5);
        }
    }
}
