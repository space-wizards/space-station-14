using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Movement;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ClimbSystem : EntitySystem, IResettingEntitySystem
    {
        private readonly HashSet<ClimbingComponent> _activeClimbers = new();

        public void AddActiveClimber(ClimbingComponent climbingComponent)
        {
            _activeClimbers.Add(climbingComponent);
        }

        public void RemoveActiveClimber(ClimbingComponent climbingComponent)
        {
            _activeClimbers.Remove(climbingComponent);
        }

        public override void Update(float frameTime)
        {
            foreach (var climber in _activeClimbers.ToArray())
            {
                climber.Update();
            }
        }

        public void Reset()
        {
            _activeClimbers.Clear();
        }
    }
}
