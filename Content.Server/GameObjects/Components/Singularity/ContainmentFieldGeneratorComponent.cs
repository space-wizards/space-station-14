using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Singularity
{
    [RegisterComponent]
    public class ContainmentFieldGeneratorComponent : Component, IExamine
    {
        public override string Name => "ContainmentFieldGenerator";

        private int _power;

        [ViewVariables]
        public int Power
        {
            get => _power;
            set => _power = Math.Clamp(value, 0, 6);
        }

        public Dictionary<IEntity, IEntity> OwnedFields = new Dictionary<IEntity, IEntity>();
        public HashSet<IEntity> ConnectedGenerators = new HashSet<IEntity>();

        private IEntityManager _entityManager;


        public override void Initialize()
        {
            base.Initialize();
            _entityManager = IoCManager.Resolve<IEntityManager>();
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            var localPos = Owner.Transform.GridPosition;
            if (localPos.X % 0.5f != 0 || localPos.Y % 0.5f != 0)
            {
                message.AddMarkup(Loc.GetString("It appears to be [color=darkred]improperly aligned with the tile.[/color]"));
            }
        }

        public void Update()
        {
            var _pos = Owner.Transform.GridPosition;

            //Remove owned fields when powered off
            if (Power == 0)
            {
                foreach (var ent in ConnectedGenerators)
                {
                    ent.GetComponent<ContainmentFieldGeneratorComponent>().ConnectedGenerators.Remove(Owner);
                }

                foreach (var ent in OwnedFields.Keys)
                {
                    ent.Delete();
                }

                OwnedFields.Clear();

                ConnectedGenerators.Clear();

            }

            HashSet<IEntity> remove = new HashSet<IEntity>();

            //Make sure connected gens have power and remove connected fields if they don't
            foreach (var ent in ConnectedGenerators)
            {
                if (!ent.IsValid() || ent.GetComponent<ContainmentFieldGeneratorComponent>().Power == 0)
                {
                    foreach(KeyValuePair<IEntity, IEntity> pair in OwnedFields)
                    {
                        if (pair.Value == ent)
                        {
                            pair.Key.Delete();
                        }
                    }

                    remove.Add(ent);
                }
            }

            foreach (var ent in remove)
            {
                ConnectedGenerators.Remove(ent);
            }

            if(Power != 0)
            {
                Power--;
            }

            //Require at least 2 power to generate new fields
            if (Power < 2)
            {
                return;
            }

            if (_pos.X % 0.5f != 0 || _pos.Y % 0.5f != 0) return;

            foreach (IEntity ent in _entityManager.GetEntitiesInRange(Owner, 4.5f))
            {
                if (ent.TryGetComponent<ContainmentFieldGeneratorComponent>(out var component) &&
                    component.Owner != Owner &&
                    component.Power != 0 &&
                    !ConnectedGenerators.Contains(component.Owner) &&
                    !component.ConnectedGenerators.Contains(Owner))
                {
                    var localPos = Owner.Transform.GridPosition;
                    var toPos = component.Owner.Transform.GridPosition;

                    bool generated = false;

                    if(localPos.Y == toPos.Y)
                    {
                        var off = new Vector2(MathF.Round(toPos.X - localPos.X), 0).Normalized;

                        var ray = new CollisionRay(Owner.Transform.WorldPosition, off, (int) CollisionGroup.MobMask);
                        var rayCastResults = IoCManager.Resolve<IPhysicsManager>().IntersectRay(Owner.Transform.MapID, ray, MathF.Abs(toPos.X - localPos.X), Owner, false);

                        bool didFindObstruction = false;

                        foreach (var result in rayCastResults)
                        {
                            if (result.HitEntity != ent)
                            {
                                didFindObstruction = true;
                                break;
                            }
                        }

                        if (didFindObstruction)
                        {
                            continue;
                        }

                        generated = true;

                        while (true)
                        {
                            localPos = localPos.Offset(off);

                            if (localPos == toPos)
                            {
                                break;
                            }

                            var newEnt = _entityManager.SpawnEntity("ContainmentField", localPos);
                            newEnt.Transform.WorldRotation = off.ToAngle();
                            OwnedFields.Add(newEnt, ent);
                        }
                    }
                    else if (localPos.X == toPos.X)
                    {
                        var off = new Vector2(0, MathF.Round(toPos.Y - localPos.Y)).Normalized;

                        var ray = new CollisionRay(Owner.Transform.WorldPosition, off, (int) CollisionGroup.MobMask);
                        var rayCastResults = IoCManager.Resolve<IPhysicsManager>().IntersectRay(Owner.Transform.MapID, ray, MathF.Abs(toPos.Y - localPos.Y), Owner, false);

                        bool didFindObstruction = false;

                        foreach (var result in rayCastResults)
                        {
                                if (result.HitEntity != ent)
                            {
                                didFindObstruction = true;
                                break;
                            }
                        }

                        if (didFindObstruction)
                        {
                            continue;
                        }

                        generated = true;

                        while (true)
                        {
                            localPos = localPos.Offset(off);

                            if (localPos == toPos)
                            {
                                break;
                            }

                            var newEnt = _entityManager.SpawnEntity("ContainmentField", localPos);
                            newEnt.Transform.WorldRotation = off.ToAngle();
                            OwnedFields.Add(newEnt, ent);
                        }
                    }

                    if (generated)
                    {
                        ConnectedGenerators.Add(ent);
                    }
                }
            }
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            foreach (var ent in OwnedFields.Keys)
            {
                ent.Delete();
            }
        }
    }
}
