using System.Collections.Generic;
using System.Linq;
using Content.Server.Climbing.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Climbing;
using Content.Shared.GameTicking;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Climbing
{
    [UsedImplicitly]
    internal sealed class ClimbSystem : SharedClimbSystem
    {
        private readonly HashSet<ClimbingComponent> _activeClimbers = new();

        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<ClimbableComponent, GetAlternativeVerbsEvent>(AddClimbVerb);
        }

        public void ForciblySetClimbing(EntityUid uid, ClimbingComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return;
            component.IsClimbing = true;
            UnsetTransitionBoolAfterBufferTime(uid, component);
        }

        private void AddClimbVerb(EntityUid uid, ClimbableComponent component, GetAlternativeVerbsEvent args)
        {
            if (!args.CanAccess || !args.CanInteract || !_actionBlockerSystem.CanMove(args.User))
                return;

            // Check that the user climb.
            if (!EntityManager.TryGetComponent(args.User, out ClimbingComponent? climbingComponent) ||
                climbingComponent.IsClimbing)
                return;

            // Add a climb verb
            Verb verb = new();
            verb.Act = () => component.TryClimb(args.User);
            verb.Text = Loc.GetString("comp-climbable-verb-climb");
            // TODO VERBS ICON add a climbing icon?
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

        public void UnsetTransitionBoolAfterBufferTime(EntityUid uid, ClimbingComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return;
            component.Owner.SpawnTimer((int) (SharedClimbingComponent.BufferTime * 1000), () =>
            {
                if (component.Deleted) return;
                component.OwnerIsTransitioning = false;
            });
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
