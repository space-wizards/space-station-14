using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.Singularity
{
    [RegisterComponent]
    public class ContainmentFieldGeneratorComponent : Component
    {
        public override string Name => "Containment Field Generator";

        public int Power = 5;

        private IEntityManager _entityManager;

        private bool generated = false;

        public override void Initialize()
        {
            base.Initialize();
            _entityManager = IoCManager.Resolve<IEntityManager>();
        }

        public void Update()
        {
            if (Power == 0) return;

            if (generated) return;

            foreach (IEntity ent in _entityManager.GetEntitiesInRange(Owner, 5f))
            {
                if (ent.TryGetComponent<ContainmentFieldGeneratorComponent>(out var component) && component.Owner != Owner && component.Power != 0)
                {
                    var localPos = Owner.Transform.GridPosition;
                    var toPos = component.Owner.Transform.GridPosition;

                    generated = true;

                    if(localPos.Y == toPos.Y)
                    {
                        do
                        {
                            var off = new Vector2(MathF.Round(toPos.X - localPos.X), 0).Normalized;
                            localPos = localPos.Offset(off);

                            var newEnt = _entityManager.SpawnEntity("ContainmentField", localPos);
                            newEnt.Transform.WorldRotation = off.ToAngle();

                        } while (localPos != toPos);
                    }
                    else if (localPos.X == toPos.X)
                    {
                        do
                        {
                            var off = new Vector2(0, MathF.Round(toPos.Y - localPos.Y)).Normalized;
                            localPos = localPos.Offset(off);

                            var newEnt = _entityManager.SpawnEntity("ContainmentField", localPos);
                            newEnt.Transform.WorldRotation = off.ToAngle();

                        } while (localPos != toPos);
                    }
                }
            }
        }
    }
}
