using System.Collections.Generic;
using System.Linq;
using Content.Server.Climbing.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Climbing;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Climbing
{
    [UsedImplicitly]
    internal sealed class ClimbSystem : SharedClimbingSystem
    {
        private readonly HashSet<ClimbingComponent> _activeClimbers = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<ClimbingComponent, BuckleMessage>(HandleBuckle);
        }

        public void AddActiveClimber(ClimbingComponent climbingComponent)
        {
            _activeClimbers.Add(climbingComponent);
        }

        public void RemoveActiveClimber(ClimbingComponent climbingComponent)
        {
            _activeClimbers.Remove(climbingComponent);
        }

        private void HandleBuckle(EntityUid uid, ClimbingComponent component, BuckleMessage args)
        {
            if (args.Buckled)
            {
                component.IsClimbing = false;
            }
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
