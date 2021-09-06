using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Climbing.Components;
using Content.Shared.Climbing;
using Content.Shared.GameTicking;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Climbing
{
    [UsedImplicitly]
    internal sealed class ClimbSystem : SharedClimbSystem
    {
        private readonly HashSet<ClimbingComponent> _activeClimbers = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<ClimbableComponent, GetAlternativeVerbsEvent>(AddClimbVerb);
        }

        private void AddClimbVerb(EntityUid uid, ClimbableComponent component, GetAlternativeVerbsEvent args)
        {
            // Check that the user interact.
            if (!args.DefaultInRangeUnobstructed || args.Hands == null)
                return;

            // Check that the user climb.
            if (!args.User.TryGetComponent(out ClimbingComponent? climbingComponent) ||
                climbingComponent.IsClimbing)
                return;

            // Add a climb verb
            Verb verb = new("climb");
            verb.Act = () => component.TryClimb(args.User);
            if (args.PrepareGUI)
            {
                verb.Text = Loc.GetString("comp-climbable-verb-climb");
                // TODO VERBS ICON add a climbing icon?
            }
            args.Verbs.Add(verb);
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
