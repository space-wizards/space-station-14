using JetBrains.Annotations;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Content.Shared.MobState.Components;
using Robust.Shared.Prototypes;
using Content.Server.DoAfter;
using System.Threading;

using static Content.Shared.HealthAnalyzer.SharedHealthAnalyzerComponent;

namespace Content.Server.HealthAnalyzer
{
    [UsedImplicitly]
    internal sealed class HealthAnalyzerSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        private CancellationTokenSource? _requestCancelTokenSource;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HealthAnalyzerComponent, ActivateInWorldEvent>(HandleActivateInWorld);
            SubscribeLocalEvent<HealthAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<TargetScanSuccessfulEvent>(OnTargetScanSuccessful);
            SubscribeLocalEvent<ScanCancelledEvent>(OnScancancelled);
        }

        private void HandleActivateInWorld(EntityUid uid, HealthAnalyzerComponent healthAnalyzer, ActivateInWorldEvent args)
        {
            OpenUserInterface(args.User, healthAnalyzer);
        }

        private void OnAfterInteract(EntityUid uid, HealthAnalyzerComponent healthAnalyzer, AfterInteractEvent args)
        {
            if (args.Target == null)
                return;

            if (!args.CanReach)
                return;

            if (healthAnalyzer.CancelToken != null)
                return;

            if (!TryComp<MobStateComponent>(args.Target, out MobStateComponent? comp))
                return;

            healthAnalyzer.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, healthAnalyzer.ScanDelay, healthAnalyzer.CancelToken.Token, target: args.Target)
            {
                BroadcastFinishedEvent = new TargetScanSuccessfulEvent(args.User, args.Target, healthAnalyzer),
                BroadcastCancelledEvent = new ScanCancelledEvent(healthAnalyzer),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                NeedHand = true
            });
        }

        private void OnTargetScanSuccessful(TargetScanSuccessfulEvent args)
        {
            args.Component.CancelToken = null;
            UpdateScannedUser(args.Component.Owner, args.User, args.Target, args.Component);
        }

        private void OpenUserInterface(EntityUid user, HealthAnalyzerComponent healthAnalyzer)
        {
            if (!TryComp<ActorComponent>(user, out ActorComponent? actor))
                return;

            healthAnalyzer.UserInterface?.Open(actor.PlayerSession);
        }

        public void UpdateScannedUser(EntityUid uid, EntityUid user, EntityUid? target, HealthAnalyzerComponent? healthAnalyzer)
        {
            if (!Resolve(uid, ref healthAnalyzer))
                return;

            if (target == null || healthAnalyzer.UserInterface == null)
                return;

            if (!TryComp<DamageableComponent>(target, out var damageable))
                return;

            OpenUserInterface(user, healthAnalyzer);
            healthAnalyzer.UserInterface?.SendMessage(new HealthAnalyzerScannedUserMessage(target));
        }

        private static void OnScancancelled(ScanCancelledEvent args)
        {
            args.HealthAnalyzer.CancelToken = null;
        }

        private sealed class ScanCancelledEvent : EntityEventArgs
        {
            public readonly HealthAnalyzerComponent HealthAnalyzer;
            public ScanCancelledEvent(HealthAnalyzerComponent healthAnalyzer)
            {
                HealthAnalyzer = healthAnalyzer;
            }
        }

        private sealed class TargetScanSuccessfulEvent : EntityEventArgs
        {
            public EntityUid User { get; }
            public EntityUid? Target { get; }
            public HealthAnalyzerComponent Component { get; }

            public TargetScanSuccessfulEvent(EntityUid user, EntityUid? target, HealthAnalyzerComponent component)
            {
                User = user;
                Target = target;
                Component = component;
            }
        }
    }
}
