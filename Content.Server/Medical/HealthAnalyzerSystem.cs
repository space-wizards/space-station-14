using JetBrains.Annotations;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Content.Shared.MobState.Components;
using Robust.Shared.Prototypes;
using Content.Server.DoAfter;

using static Content.Shared.HealthAnalyzer.SharedHealthAnalyzerComponent;

namespace Content.Server.HealthAnalyzer
{
    [UsedImplicitly]
    internal sealed class HealthAnalyzerSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HealthAnalyzerComponent, ActivateInWorldEvent>(HandleActivateInWorld);
            SubscribeLocalEvent<HealthAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<TargetScanSuccessfulEvent>(OnTargetScanSuccessful);
        }

        private void HandleActivateInWorld(EntityUid uid, HealthAnalyzerComponent healthAnalyzer, ActivateInWorldEvent args)
        {
            OpenUserInterface(args.User, healthAnalyzer);
        }

        private void OnAfterInteract(EntityUid uid, HealthAnalyzerComponent HealthAnalyzer, AfterInteractEvent args)
        {
            if (args.Target == null)
                return;

            if (!args.CanReach)
                return;

            if (!TryComp<MobStateComponent>(args.Target, out MobStateComponent? comp))
                return;

            _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, HealthAnalyzer.ScanDelay, target: args.Target)
            {
                BroadcastFinishedEvent = new TargetScanSuccessfulEvent(args.User, args.Target, HealthAnalyzer),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
            });
        }

        private void OnTargetScanSuccessful(TargetScanSuccessfulEvent args)
        {
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
