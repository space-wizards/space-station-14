using Content.Server.DoAfter;
using Content.Server.Disease;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.MedicalScanner;
using Content.Shared.Mobs.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Medical
{
    public sealed class HealthAnalyzerSystem : EntitySystem
    {
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly DiseaseSystem _disease = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HealthAnalyzerComponent, ActivateInWorldEvent>(HandleActivateInWorld);
            SubscribeLocalEvent<HealthAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<HealthAnalyzerComponent, DoAfterEvent>(OnDoAfter);
        }

        private void HandleActivateInWorld(EntityUid uid, HealthAnalyzerComponent healthAnalyzer, ActivateInWorldEvent args)
        {
            if (!TryComp<ActorComponent>(args.User, out var actor))
                return;

            _uiSystem.TryOpen(uid, HealthAnalyzerUiKey.Key, actor.PlayerSession);
        }

        private void OnAfterInteract(EntityUid uid, HealthAnalyzerComponent healthAnalyzer, AfterInteractEvent args)
        {
            if (args.Target == null || !args.CanReach || !HasComp<MobStateComponent>(args.Target))
                return;

            _audio.PlayPvs(healthAnalyzer.ScanningBeginSound, uid);

            _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, healthAnalyzer.ScanDelay, target: args.Target, used:uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                NeedHand = true
            });
        }

        private void OnDoAfter(EntityUid uid, HealthAnalyzerComponent component, DoAfterEvent args)
        {
            if (args.Handled || args.Cancelled || args.Args.Target == null)
                return;

            _audio.PlayPvs(component.ScanningEndSound, args.Args.User);

            UpdateScannedUser(uid, args.Args.User, args.Args.Target.Value, component);
            // Below is for the traitor item
            // Piggybacking off another component's doafter is complete CBT so I gave up
            // and put it on the same component
            if (string.IsNullOrEmpty(component.Disease))
            {
                args.Handled = true;
                return;
            }

            _disease.TryAddDisease(args.Args.Target.Value, component.Disease);

            if (args.Args.User == args.Args.Target)
            {
                _popupSystem.PopupEntity(Loc.GetString("disease-scanner-gave-self", ("disease", component.Disease)),
                    args.Args.User, args.Args.User);
            }


            else
            {
                _popupSystem.PopupEntity(Loc.GetString("disease-scanner-gave-other", ("target", Identity.Entity(args.Args.Target.Value, EntityManager)),
                    ("disease", component.Disease)), args.Args.User, args.Args.User);
            }

            args.Handled = true;
        }

        public void UpdateScannedUser(EntityUid uid, EntityUid user, EntityUid? target, HealthAnalyzerComponent? healthAnalyzer)
        {
            if (!Resolve(uid, ref healthAnalyzer))
                return;

            if (target == null)
                return;

            if (!HasComp<DamageableComponent>(target))
                return;

            if (!TryComp<ActorComponent>(user, out var actor))
                return;

            if (_uiSystem.GetUiOrNull(uid, HealthAnalyzerUiKey.Key) is not { } ui)
                return;

            _uiSystem.OpenUi(ui, actor.PlayerSession);
            _uiSystem.SendUiMessage(ui, new HealthAnalyzerScannedUserMessage(target));
        }
    }
}
