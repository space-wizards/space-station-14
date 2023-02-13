using Content.Server.Administration.Logs;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.UserInterface;
using Content.Shared.AirlockPainter;
using Content.Shared.AirlockPainter.Prototypes;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;

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
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<AirlockPainterComponent, AfterInteractEvent>(AfterInteractOn);
            SubscribeLocalEvent<AirlockPainterComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<AirlockPainterComponent, AirlockPainterSpritePickedMessage>(OnSpritePicked);
            SubscribeLocalEvent<AirlockPainterDoAfterComplete>(OnDoAfterComplete);
            SubscribeLocalEvent<AirlockPainterDoAfterCancelled>(OnDoAfterCancelled);
        }

        private void OnDoAfterComplete(AirlockPainterDoAfterComplete ev)
        {
            ev.Component.IsSpraying = false;
            if (TryComp<AppearanceComponent>(ev.Target, out var appearance) &&
                TryComp(ev.Target, out PaintableAirlockComponent? _))
            {
                SoundSystem.Play(ev.Component.SpraySound.GetSound(), Filter.Pvs(ev.UsedTool, entityManager:EntityManager), ev.UsedTool);
                _appearance.SetData(ev.Target, DoorVisuals.BaseRSI, ev.Sprite, appearance);

                // Log success
                _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(ev.User):user} painted {ToPrettyString(ev.Target):target}");
            }
        }

        private void OnDoAfterCancelled(AirlockPainterDoAfterCancelled ev)
        {
            ev.Component.IsSpraying = false;
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
            var doAfterEventArgs = new DoAfterEventArgs(args.User, component.SprayTime, default, target)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                NeedHand = true,
                BroadcastFinishedEvent = new AirlockPainterDoAfterComplete(uid, target, sprite, component, args.User),
                BroadcastCancelledEvent = new AirlockPainterDoAfterCancelled(component),
            };
            _doAfterSystem.DoAfter(doAfterEventArgs);

            // Log attempt
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.User):user} is painting {ToPrettyString(uid):target} to '{style}' at {Transform(uid).Coordinates:targetlocation}");
        }

        private sealed class AirlockPainterDoAfterComplete : EntityEventArgs
        {
            public readonly EntityUid User;
            public readonly EntityUid UsedTool;
            public readonly EntityUid Target;
            public readonly string Sprite;
            public readonly AirlockPainterComponent Component;

            public AirlockPainterDoAfterComplete(EntityUid usedTool, EntityUid target, string sprite,
                AirlockPainterComponent component, EntityUid user)
            {
                User = user;
                UsedTool = usedTool;
                Target = target;
                Sprite = sprite;
                Component = component;
            }
        }

        private sealed class AirlockPainterDoAfterCancelled : EntityEventArgs
        {
            public readonly AirlockPainterComponent Component;

            public AirlockPainterDoAfterCancelled(AirlockPainterComponent component)
            {
                Component = component;
            }
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
