using Content.Server.Explosion.EntitySystems;
using Content.Server.Pointing.Components;
using Content.Shared.Pointing.Components;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Server.Pointing.EntitySystems
{
    [UsedImplicitly]
    internal sealed class RoguePointingSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ExplosionSystem _explosion = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

        private EntityUid? RandomNearbyPlayer(EntityUid uid, RoguePointingArrowComponent? component = null, TransformComponent? transform = null)
        {
            if (!Resolve(uid, ref component, ref transform))
                return null;

            var targets = new List<Entity<PointingArrowAngeringComponent>>();
            var query = EntityQueryEnumerator<PointingArrowAngeringComponent>();
            while (query.MoveNext(out var angeringUid, out var angeringComp))
            {
                targets.Add((angeringUid, angeringComp));
            }

            if (targets.Count == 0)
                return null;

            var angering = _random.Pick(targets);
            angering.Comp.RemainingAnger -= 1;
            if (angering.Comp.RemainingAnger <= 0)
                RemComp<PointingArrowAngeringComponent>(angering);

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
            var query = EntityQueryEnumerator<RoguePointingArrowComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var component, out var transform))
            {
                component.Chasing ??= RandomNearbyPlayer(uid, component, transform);

                if (component.Chasing is not {Valid: true} chasing || Deleted(chasing))
                {
                    EntityManager.QueueDeleteEntity(uid);
                    continue;
                }

                component.TurningDelay -= frameTime;
                var (transformPos, transformRot) = _transformSystem.GetWorldPositionRotation(transform);

                if (component.TurningDelay > 0)
                {
                    var difference = _transformSystem.GetWorldPosition(chasing) - transformPos;
                    var angle = difference.ToAngle();
                    var adjusted = angle.Degrees + 90;
                    var newAngle = Angle.FromDegrees(adjusted);

                    _transformSystem.SetWorldRotation(transform, newAngle);

                    UpdateAppearance(uid, component, transform);
                    continue;
                }

                _transformSystem.SetWorldRotation(transform, transformRot + Angle.FromDegrees(20));

                UpdateAppearance(uid, component, transform);

                var toChased = _transformSystem.GetWorldPosition(chasing) - transformPos;

                _transformSystem.SetWorldPosition((uid, transform), transformPos + (toChased * frameTime * component.ChasingSpeed));

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
