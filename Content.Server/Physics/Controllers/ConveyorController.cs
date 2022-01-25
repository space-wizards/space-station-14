using System;
using System.Collections.Generic;
using Content.Server.Conveyor;
using Content.Server.Recycling.Components;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Controllers;

namespace Content.Server.Physics.Controllers
{
    internal sealed class ConveyorController : VirtualController
    {
        [Dependency] private readonly ConveyorSystem _conveyor = default!;

        public override void Initialize()
        {
            UpdatesAfter.Add(typeof(MoverController));

            base.Initialize();
        }

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);
            // TODO: This won't work if someone wants a massive fuckoff conveyor so look at using StartCollide or something.
            foreach (var (comp, xform) in EntityManager.EntityQuery<ConveyorComponent, TransformComponent>())
            {
                Convey(_conveyor, comp, xform, frameTime);
            }
        }

        private void Convey(ConveyorSystem system, ConveyorComponent comp, TransformComponent xform, float frameTime)
        {
            // Use an event for conveyors to know what needs to run
            if (!system.CanRun(comp))
            {
                return;
            }

            var speed = comp.Speed;

            if (speed <= 0f) return;

            var direction = xform.WorldRotation.ToWorldVec();

            foreach (var (entity, physics) in _conveyor.GetEntitiesToMove(comp))
            {
                var conveyance = direction * comp.Speed;
                physics.LinearVelocity += conveyance;
            }
        }
    }
}
