using System.Linq;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Pointing.Components;
using Content.Shared.Pointing.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Random;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Server.Pointing.EntitySystems
{
    [UsedImplicitly]
    internal sealed class RoguePointingSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ExplosionSystem _explosion = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        private EntityUid? RandomNearbyPlayer(EntityUid uid, RoguePointingArrowComponent? component = null, TransformComponent? transform = null)
        {
            if (!Resolve(uid, ref component, ref transform))
                return null;

            var targets = EntityQuery<PointingArrowAngeringComponent>().ToList();

            if (targets.Count == 0)
                return null;

            var angering = _random.Pick(targets);
            angering.RemainingAnger -= 1;
            if (angering.RemainingAnger <= 0)
                RemComp<PointingArrowAngeringComponent>(uid);

            return angering.Owner;
        }

        private void UpdateAppearance(EntityUid uid, RoguePointingArrowComponent? component = null, TransformComponent? transform = null, AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref component, ref transform, ref appearance) || component.Chasing == null)
                return;

            _appearance.SetData(uid, RoguePointingArrowVisuals.Rotation, transform.LocalRotation.Degrees, appearance);
        }

        public void SetTarget(EntityUid arrow, EntityUid target, RoguePointingArrowComponent? component = null)
        {
            if (!Resolve(arrow, ref component))
                throw new ArgumentException("Input was not a rogue pointing arrow!", nameof(arrow));

            component.Chasing = target;
        }

        public override void Update(float frameTime)
        {
            foreach (var (component, transform) in EntityManager.EntityQuery<RoguePointingArrowComponent, TransformComponent>())
            {
                var uid = component.Owner;
                component.Chasing ??= RandomNearbyPlayer(uid, component, transform);

                if (component.Chasing is not {Valid: true} chasing || Deleted(chasing))
                {
                    EntityManager.QueueDeleteEntity(uid);
                    continue;
                }

                component.TurningDelay -= frameTime;

                if (component.TurningDelay > 0)
                {
                    var difference = EntityManager.GetComponent<TransformComponent>(chasing).WorldPosition - transform.WorldPosition;
                    var angle = difference.ToAngle();
                    var adjusted = angle.Degrees + 90;
                    var newAngle = Angle.FromDegrees(adjusted);

                    transform.WorldRotation = newAngle;

                    UpdateAppearance(uid, component, transform);
                    continue;
                }

                transform.WorldRotation += Angle.FromDegrees(20);

                UpdateAppearance(uid, component, transform);

                var toChased = EntityManager.GetComponent<TransformComponent>(chasing).WorldPosition - transform.WorldPosition;

                transform.WorldPosition += toChased * frameTime * component.ChasingSpeed;

                component.ChasingTime -= frameTime;

                if (component.ChasingTime > 0)
                {
                    continue;
                }


                _explosion.QueueExplosion(uid, ExplosionSystem.DefaultExplosionPrototypeId, 50, 3, 10);
                EntityManager.QueueDeleteEntity(uid);
            }
        }
    }
}
