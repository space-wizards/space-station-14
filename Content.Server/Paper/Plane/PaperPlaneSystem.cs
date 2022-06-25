using Content.Server.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Physics.Dynamics;
using System.Threading;

namespace Content.Server.Paper.Plane
{
    public sealed class PaperPlaneSystem : EntitySystem
    {
        [Dependency] private SharedHandsSystem _sharedHandsSystem = default!;
        [Dependency] private DoAfterSystem _doAfterSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PaperComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerbPaper);
            SubscribeLocalEvent<PaperComponent, FoldPlaneEvent>(OnFoldPlaneEvent);
            SubscribeLocalEvent<FoldPlanceCancelledEvent>(OnFoldPlaneCancel);

            SubscribeLocalEvent<PaperPlaneComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerbPlane);
            SubscribeLocalEvent<PaperPlaneComponent, UnfoldPlaneEvent>(OnUnfoldPlane);
            SubscribeLocalEvent<UnfoldPlaneCancelledEvent>(OnUnfoldPlaneCancel);

            SubscribeLocalEvent<PaperPlaneComponent, StartCollideEvent>(OnCollideEvent);
        }

        private void OnAltVerbPlane(EntityUid uid, PaperPlaneComponent plane, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            AlternativeVerb verb = new()
            {
                Act = () => TryUnfoldPlane(plane.Owner, plane, args),
                Text = Loc.GetString("paper-component-verb-unfold-plane"),
                IconTexture = "/Textures/Interface/VerbIcons/fold.svg.192dpi.png",
            };

            args.Verbs.Add(verb);
        }

        private void TryUnfoldPlane(EntityUid uid, PaperPlaneComponent plane, GetVerbsEvent<AlternativeVerb> args)
        {
            if (plane.CancelToken != null)
                return;

            plane.CancelToken = new CancellationTokenSource();

            _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, plane.FoldDelay, plane.CancelToken.Token, plane.Owner)
            {
                BreakOnDamage = true,
                BreakOnStun = true,
                TargetFinishedEvent = new UnfoldPlaneEvent(plane, args.User),
                TargetCancelledEvent = new UnfoldPlaneCancelledEvent(plane),
                NeedHand = true,
            });
        }

        private void OnUnfoldPlane(EntityUid uid, PaperPlaneComponent plane, UnfoldPlaneEvent args)
        {
            args.Plane.CancelToken = null;

            if (args.Plane.PaperContainer.ContainedEntity is not { Valid: true } paper)
            {
                //default to blank paper, i.e. a paper plane was spawned instead of folded
                paper = Spawn("Paper", Transform(args.User).MapPosition);
            }

            args.Plane.PaperContainer.Remove(paper);

            //drop the plane if holding, so the paper can go into the same hand
            if (_sharedHandsSystem.IsHolding(args.User, args.Plane.Owner, out var hand))
                _sharedHandsSystem.TryDrop(args.User, hand);

            _sharedHandsSystem.PickupOrDrop(args.User, paper);

            EntityManager.QueueDeleteEntity(args.Plane.Owner);
        }

        private void OnAltVerbPaper(EntityUid uid, PaperComponent paper, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            AlternativeVerb verb = new()
            {
                Act = () => TryFoldPlane(paper.Owner, paper, args),
                Text = Loc.GetString("paper-component-verb-fold-plane"),
                IconTexture = "/Textures/Interface/VerbIcons/fold.svg.192dpi.png",
            };

            args.Verbs.Add(verb);
        }

        private void TryFoldPlane(EntityUid uid, PaperComponent paper, GetVerbsEvent<AlternativeVerb> args)
        {
            if (paper.CancelToken != null)
                return;

            paper.CancelToken = new CancellationTokenSource();

            _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, paper.FoldDelay, paper.CancelToken.Token, paper.Owner)
            {
                BreakOnDamage = true,
                BreakOnStun = true,
                TargetFinishedEvent = new FoldPlaneEvent(paper, args.User),
                TargetCancelledEvent = new FoldPlanceCancelledEvent(paper),
                NeedHand = true,
            });
        }

        private void OnFoldPlaneEvent(EntityUid uid, PaperComponent paper, FoldPlaneEvent args)
        {
            args.Paper.CancelToken = null;

            var ent = Spawn("PaperPlane", Transform(args.User).MapPosition);
            PaperPlaneComponent plane = Comp<PaperPlaneComponent>(ent);
            plane.PaperContainer.Insert(paper.Owner);

            _sharedHandsSystem.PickupOrDrop(args.User, plane.Owner);
        }

        //avoid bouncing
        private void OnCollideEvent(EntityUid uid, PaperPlaneComponent plane, StartCollideEvent args)
        {
            if (TryComp<PhysicsComponent>(plane.Owner, out var physics))
                physics.Momentum = Vector2.Zero;
        }

        private void OnUnfoldPlaneCancel(UnfoldPlaneCancelledEvent args)
        {
            args.Plane.CancelToken = null;
        }

        private void OnFoldPlaneCancel(FoldPlanceCancelledEvent args)
        {
            args.Paper.CancelToken = null;
        }

        public sealed class FoldPlaneEvent : EntityEventArgs
        {
            public readonly PaperComponent Paper;
            public readonly EntityUid User;

            public FoldPlaneEvent(PaperComponent paper, EntityUid user)
            {
                Paper = paper;
                User = user;
            }
        }

        public sealed class UnfoldPlaneEvent : EntityEventArgs
        {
            public readonly PaperPlaneComponent Plane;
            public readonly EntityUid User;

            public UnfoldPlaneEvent(PaperPlaneComponent plane, EntityUid user)
            {
                Plane = plane;
                User = user;
            }
        }

        public sealed class UnfoldPlaneCancelledEvent : EntityEventArgs
        {
            public readonly PaperPlaneComponent Plane;

            public UnfoldPlaneCancelledEvent(PaperPlaneComponent plane)
            {
                Plane = plane;
            }
        }

        public sealed class FoldPlanceCancelledEvent : EntityEventArgs
        {
            public readonly PaperComponent Paper;

            public FoldPlanceCancelledEvent(PaperComponent paper)
            {
                Paper = paper;
            }
        }
    }
}
