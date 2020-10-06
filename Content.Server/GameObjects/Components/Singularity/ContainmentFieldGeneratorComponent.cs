using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Projectiles;
using Content.Server.Utility;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Singularity
{
    [RegisterComponent]
    public class ContainmentFieldGeneratorComponent : Component, ICollideBehavior
    {
        [Dependency] private IPhysicsManager _physicsManager;
        [Dependency] private IEntityManager _entityManager;

        public override string Name => "ContainmentFieldGenerator";

        private int _power;

        [ViewVariables]
        public int Power
        {
            get => _power;
            set {
                _power = Math.Clamp(value, 0, 6);
                OnPowerChange();
            }
        }

        private CollidableComponent _collidableComponent;

        public Dictionary<IEntity, ContainmentFieldGeneratorComponent> OwnedFields = new Dictionary<IEntity, ContainmentFieldGeneratorComponent>();

        public Dictionary<ContainmentFieldGeneratorComponent, Direction> ConnectedGenerators = new Dictionary<ContainmentFieldGeneratorComponent, Direction>();

        public override void Initialize()
        {
            base.Initialize();
            if (!Owner.TryGetComponent(out _collidableComponent))
            {
                Logger.Error("ContainmentFieldGeneratorComponent created with no CollidableComponent");
                return;
            }
            _collidableComponent.AnchoredChanged += OnAnchoredChanged;
        }

        private void OnAnchoredChanged()
        {
            if(_collidableComponent.Anchored)
            {
                Owner.SnapToGrid();
            }
            else
            {
                RemoveFields();
            }
        }

        private void OnPowerChange()
        {
            if (Power == 0)
            {
                RemoveFields();
            }else if (Power >= 2)
            {
                GenerateFields();
            }
        }

        private void RemoveFields()
        {
            foreach (var (comp, _) in ConnectedGenerators)
            {
                comp.ConnectedGenerators.Remove(this);
                comp.ValidateOwnedFields();
            }
            ConnectedGenerators.Clear();

            ValidateOwnedFields();
            OwnedFields.Clear();
        }

        private void GenerateFields()
        {
            if(!_collidableComponent.Anchored) return;

            var pos = Owner.Transform.Coordinates;

            foreach (var direction in new []{Direction.North, Direction.East, Direction.South, Direction.West})
            {
                if(ConnectedGenerators.Count() > 1) return;
                if(ConnectedGenerators.ContainsValue(direction)) continue;
                var dirVec = direction.ToVec();
                var ray = new CollisionRay(Owner.Transform.WorldPosition, dirVec, (int) CollisionGroup.MobMask);
                var rawRayCastResults = _physicsManager.IntersectRay(Owner.Transform.MapID, ray, 4.5f, Owner, false);

                var rayCastResults = rawRayCastResults as RayCastResults[] ?? rawRayCastResults.ToArray();
                if(!rayCastResults.Any()) continue;

                RayCastResults? closestResult = null;
                var smallestDist = 4.5f;
                foreach (var res in rayCastResults)
                {
                    if (res.Distance > smallestDist) continue;

                    smallestDist = res.Distance;
                    closestResult = res;
                }
                if(closestResult == null) continue;
                var ent = closestResult.Value.HitEntity;
                if (!ent.TryGetComponent<ContainmentFieldGeneratorComponent>(
                        out var fieldGeneratorComponent) || fieldGeneratorComponent.Owner == Owner ||
                    fieldGeneratorComponent.Power == 0 ||
                    ConnectedGenerators.ContainsKey(fieldGeneratorComponent) ||
                    fieldGeneratorComponent.ConnectedGenerators.ContainsKey(this) ||
                    !ent.TryGetComponent<CollidableComponent>(out var collidableComponent) ||
                    !collidableComponent.Anchored)
                {
                    continue;
                }

                var stopDist = (dirVec * closestResult.Value.Distance).Length;
                var currentOffset = dirVec;
                while (currentOffset.Length < stopDist)
                {
                    var currentCoords = pos.Offset(currentOffset);
                    var newEnt = _entityManager.SpawnEntity("ContainmentField", currentCoords);
                    newEnt.Transform.WorldRotation = dirVec.ToAngle();
                    OwnedFields.Add(newEnt, fieldGeneratorComponent);

                    currentOffset += dirVec;
                }

                ConnectedGenerators.Add(fieldGeneratorComponent, direction);
                fieldGeneratorComponent.ConnectedGenerators.Add(this, direction.GetOpposite());
            }
        }

        private void ValidateOwnedFields()
        {
            var toRemove = new HashSet<IEntity>();
            foreach (var (entity, comp) in OwnedFields.Where(ownedFieldPair => !ConnectedGenerators.ContainsKey(ownedFieldPair.Value)))
            {
                entity.Delete();
                toRemove.Add(entity);
            }

            foreach (var entity in toRemove)
            {
                OwnedFields.Remove(entity);
            }
        }

        public void Update()
        {
            Power--;
        }

        public override void OnRemove()
        {
            RemoveFields();
            base.OnRemove();
        }

        public void CollideWith(IEntity collidedWith)
        {
            if(collidedWith.TryGetComponent<EmitterBoltComponent>(out var _))
            {
                Power++;
            }
        }
    }
}
