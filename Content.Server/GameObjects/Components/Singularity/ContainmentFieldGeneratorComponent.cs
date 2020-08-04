using System;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Singularity
{
    [RegisterComponent]
    public class ContainmentFieldGeneratorComponent : Component, IExamine
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

            if (Power == 0) return;

            if (generated) return;

            if (_pos.X % 0.5f != 0 || _pos.Y % 0.5f != 0)

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
