using Content.Server.DoAfter;
using Content.Server.UserInterface;
using Content.Shared.Movement.Components;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;
using System.Threading;
using static Content.Shared.Paper.SharedPaperComponent;

namespace Content.Server.Paper.Plane
{
    public sealed class PaperPlaneSystem : EntitySystem
    {
        [Dependency] private DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;

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
            SubscribeLocalEvent<PaperPlaneComponent, ActivatableUIOpenAttemptEvent>(OnPaperUIOpenEvent);
            SubscribeLocalEvent<PaperPlaneComponent, LandEvent>(OnLand);
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

            if (TryComp<AppearanceComponent>(uid, out var appearance) &&
                TryComp<PaperComponent>(uid, out var paper))
            {
                PaperStatus status = paper.Content != "" ? PaperStatus.Written : PaperStatus.Blank;
                    appearance.SetData(PaperVisuals.Status, status);
            }

            _tagSystem.RemoveTags(plane.Owner, plane.Tags);

            _entityManager.RemoveComponent<PaperPlaneComponent>(plane.Owner);
        }

        private void OnAltVerbPaper(EntityUid uid, PaperComponent paper, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (HasComp<PaperPlaneComponent>(paper.Owner))
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

            var plane = _entityManager.AddComponent<PaperPlaneComponent>(paper.Owner);

            if (TryComp<AppearanceComponent>(uid, out var appearance))
                appearance.SetData(PaperVisuals.Status, PaperStatus.Plane);

            _tagSystem.AddTags(paper.Owner, plane.Tags);
        }

        private void OnCollideEvent(EntityUid uid, PaperPlaneComponent plane, StartCollideEvent args)
        {
            //ignore mobs
            if (args.OtherFixture.Body.BodyType == Robust.Shared.Physics.BodyType.KinematicController)
                return;

            // avoid bouncing off of walls, unless weightless
            if (TryComp<PhysicsComponent>(plane.Owner, out var physics) && !plane.Owner.IsWeightless(physics, entityManager: EntityManager))
                physics.Momentum = Vector2.Zero;
        }

        public void OnLand(EntityUid uid, PaperPlaneComponent plane, LandEvent args)
        {
            //keep on floatin' baby
            if (TryComp<PhysicsComponent>(uid, out var physics))
                physics.BodyStatus = BodyStatus.InAir;
        }

        private void OnPaperUIOpenEvent(EntityUid uid, PaperPlaneComponent plane, ActivatableUIOpenAttemptEvent args)
        {
            //don't open paper UI when folded. User must unfold first
            args.Cancel();
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
