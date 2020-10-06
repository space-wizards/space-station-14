using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Projectiles;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Singularity
{
    [RegisterComponent]
    public class ContainmentFieldGeneratorComponent : Component, IExamine, ICollideBehavior
    {
        [Dependency] private IPhysicsManager _physicsManager;
        [Dependency] private IEntityManager _entityManager;

        public override string Name => "ContainmentFieldGenerator";

        private int _power = 6; //todo 0

        [ViewVariables]
        public int Power
        {
            get => _power;
            set {
                _power = 6; //todo Math.Clamp(value, 0, 6);
                OnPowerChange();
            }
        }

        public Dictionary<IEntity, ContainmentFieldGeneratorComponent> OwnedFields = new Dictionary<IEntity, ContainmentFieldGeneratorComponent>();

        public HashSet<ContainmentFieldGeneratorComponent> ConnectedGenerators = new HashSet<ContainmentFieldGeneratorComponent>();
        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            var localPos = Owner.Transform.Coordinates;
            if (localPos.X % 0.5f != 0 || localPos.Y % 0.5f != 0)  //todo center on anchor
            {
                message.AddMarkup(Loc.GetString("It appears to be [color=darkred]improperly aligned with the tile.[/color]"));
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
            foreach (var ent in ConnectedGenerators)
            {
                ent.ConnectedGenerators.Remove(this);
                ent.ValidateOwnedFields();
            }
            ConnectedGenerators.Clear();

            ValidateOwnedFields();
            OwnedFields.Clear();

        }

        private void GenerateFields()
        {
            var pos = Owner.Transform.Coordinates;

            if (pos.X % 0.5f != 0 || pos.Y % 0.5f != 0) return; //todo center on anchor

            foreach (var direction in new []{Direction.North, Direction.East, Direction.South, Direction.West}) //todo skip dirs if we already have something in that direction
            {
                if(ConnectedGenerators.Count() > 1) return;
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
                    ConnectedGenerators.Contains(fieldGeneratorComponent) ||
                    fieldGeneratorComponent.ConnectedGenerators.Contains(this))
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

                ConnectedGenerators.Add(fieldGeneratorComponent);
                fieldGeneratorComponent.ConnectedGenerators.Add(this);
            }
        }

        public void ValidateOwnedFields()
        {
            HashSet<IEntity> toRemove = new HashSet<IEntity>();
            foreach (var ownedFieldPair in OwnedFields)
            {
                if(ConnectedGenerators.Contains(ownedFieldPair.Value)) continue;

                ownedFieldPair.Key.Delete();
                toRemove.Add(ownedFieldPair.Key);
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
                Power += 1;
            }
        }
    }
}
