using System.Collections.Generic;
using System.Linq;
using Content.Server.Climbing.Components;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Climbing
{
    [UsedImplicitly]
    internal sealed class ClimbSystem : EntitySystem
    {
        private readonly HashSet<ClimbingComponent> _activeClimbers = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
        }

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

        public void Reset(RoundRestartCleanupEvent ev)
        {
            _activeClimbers.Clear();
        }
    }
}
