using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Server.UserInterface;
using Content.Shared.AirlockPainter;
using Content.Shared.AirlockPainter.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Server.AirlockPainter
{
    /// <summary>
    /// A system for painting airlocks using airlock painter
    /// </summary>
    [UsedImplicitly]
    public sealed class AirlockPainterSystem : SharedAirlockPainterSystem
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<AirlockPainterComponent, AfterInteractEvent>(AfterInteractOn);
            SubscribeLocalEvent<AirlockPainterComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<AirlockPainterComponent, AirlockPainterSpritePickedMessage>(OnSpritePicked);
            SubscribeLocalEvent<AirlockPainterComponent, AirlockPainterDoAfterEvent>(OnDoAfter);
        }

        private void OnDoAfter(EntityUid uid, AirlockPainterComponent component, AirlockPainterDoAfterEvent args)
        {
            component.IsSpraying = false;

            if (args.Handled || args.Cancelled)
                return;

            if (args.Args.Target != null)
            {
                _audio.PlayPvs(component.SpraySound, uid);
                _appearance.SetData(args.Args.Target.Value, DoorVisuals.BaseRSI, args.Sprite);
                _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.Args.User):user} painted {ToPrettyString(args.Args.Target.Value):target}");
            }

            args.Handled = true;
        }

        private void OnActivate(EntityUid uid, AirlockPainterComponent component, ActivateInWorldEvent args)
        {
            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;
            DirtyUI(uid, component);
            component.Owner.GetUIOrNull(AirlockPainterUiKey.Key)?.Open(actor.PlayerSession);
            args.Handled = true;
        }

        private void AfterInteractOn(EntityUid uid, AirlockPainterComponent component, AfterInteractEvent args)
        {
            if (component.IsSpraying || args.Target is not { Valid: true } target || !args.CanReach)
                return;

            if (!EntityManager.TryGetComponent<PaintableAirlockComponent>(target, out var airlock))
                return;

            if (!_prototypeManager.TryIndex<AirlockGroupPrototype>(airlock.Group, out var grp))
            {
                Logger.Error("Group not defined: %s", airlock.Group);
                return;
            }

            string style = Styles[component.Index];
            if (!grp.StylePaths.TryGetValue(style, out var sprite))
            {
                string msg = Loc.GetString("airlock-painter-style-not-available");
                _popupSystem.PopupEntity(msg, args.User, args.User);
                return;
            }
            component.IsSpraying = true;

            var doAfterEventArgs = new DoAfterArgs(args.User, component.SprayTime, new AirlockPainterDoAfterEvent(sprite), uid, target: target, used: uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                NeedHand = true,
            };
            _doAfterSystem.TryStartDoAfter(doAfterEventArgs);

            // Log attempt
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.User):user} is painting {ToPrettyString(uid):target} to '{style}' at {Transform(uid).Coordinates:targetlocation}");
        }

        private void OnSpritePicked(EntityUid uid, AirlockPainterComponent component, AirlockPainterSpritePickedMessage args)
        {
            component.Index = args.Index;
            DirtyUI(uid, component);
        }

        private void DirtyUI(EntityUid uid,
            AirlockPainterComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            _userInterfaceSystem.TrySetUiState(uid, AirlockPainterUiKey.Key,
                new AirlockPainterBoundUserInterfaceState(component.Index));
        }
    }
}
