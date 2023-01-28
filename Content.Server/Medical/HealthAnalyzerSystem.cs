using System.Threading;
using Content.Server.DoAfter;
using Content.Server.Medical.Components;
using Content.Server.Disease;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using static Content.Shared.MedicalScanner.SharedHealthAnalyzerComponent;

namespace Content.Server.Medical
{
    public sealed class HealthAnalyzerSystem : EntitySystem
    {
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly DiseaseSystem _disease = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HealthAnalyzerComponent, ActivateInWorldEvent>(HandleActivateInWorld);
            SubscribeLocalEvent<HealthAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<TargetScanSuccessfulEvent>(OnTargetScanSuccessful);
            SubscribeLocalEvent<ScanCancelledEvent>(OnScanCancelled);
        }

        private void HandleActivateInWorld(EntityUid uid, HealthAnalyzerComponent healthAnalyzer, ActivateInWorldEvent args)
        {
            OpenUserInterface(args.User, healthAnalyzer);
        }

        private void OnAfterInteract(EntityUid uid, HealthAnalyzerComponent healthAnalyzer, AfterInteractEvent args)
        {
            if (healthAnalyzer.CancelToken != null)
            {
                healthAnalyzer.CancelToken.Cancel();
                healthAnalyzer.CancelToken = null;
                return;
            }

            if (args.Target == null)
                return;

            if (!args.CanReach)
                return;

            if (healthAnalyzer.CancelToken != null)
                return;

            if (!HasComp<MobStateComponent>(args.Target))
                return;

            healthAnalyzer.CancelToken = new CancellationTokenSource();

            _audio.PlayPvs(healthAnalyzer.ScanningBeginSound, uid);

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

            _audio.PlayPvs(args.Component.ScanningEndSound, args.User);

            UpdateScannedUser(args.Component.Owner, args.User, args.Target, args.Component);
            // Below is for the traitor item
            // Piggybacking off another component's doafter is complete CBT so I gave up
            // and put it on the same component
            if (string.IsNullOrEmpty(args.Component.Disease) || args.Target == null)
                return;

            _disease.TryAddDisease(args.Target.Value, args.Component.Disease);

            if (args.User == args.Target)
            {
                _popupSystem.PopupEntity(Loc.GetString("disease-scanner-gave-self", ("disease", args.Component.Disease)),
                    args.User, args.User);
                return;
            }
            _popupSystem.PopupEntity(Loc.GetString("disease-scanner-gave-other", ("target", Identity.Entity(args.Target.Value, EntityManager)), ("disease", args.Component.Disease)),
                args.User, args.User);
        }

        private void OpenUserInterface(EntityUid user, HealthAnalyzerComponent healthAnalyzer)
        {
            if (!TryComp<ActorComponent>(user, out var actor))
                return;

            healthAnalyzer.UserInterface?.Open(actor.PlayerSession);
        }

        public void UpdateScannedUser(EntityUid uid, EntityUid user, EntityUid? target, HealthAnalyzerComponent? healthAnalyzer)
        {
            if (!Resolve(uid, ref healthAnalyzer))
                return;

            if (target == null || healthAnalyzer.UserInterface == null)
                return;

            if (!HasComp<DamageableComponent>(target))
                return;

            OpenUserInterface(user, healthAnalyzer);
            healthAnalyzer.UserInterface?.SendMessage(new HealthAnalyzerScannedUserMessage(target));
        }

        private static void OnScanCancelled(ScanCancelledEvent args)
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
