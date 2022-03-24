using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.UserInterface;
using Content.Shared.AirlockPainter;
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
    public sealed class AirlockPainterSystem : EntitySystem
    {

        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

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
                TryComp<PaintableAirlockComponent>(ev.Target, out PaintableAirlockComponent? airlock))
            {
                SoundSystem.Play(Filter.Pvs(ev.User), ev.Component.SpraySound.GetSound(), ev.User);
                appearance.SetData(DoorVisuals.BaseRSI, ev.Sprite);
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
            component.Owner.GetUIOrNull(AirlockPainterUiKey.Key)?.Open(actor.PlayerSession);
            args.Handled = true;
        }

        private void AfterInteractOn(EntityUid uid, AirlockPainterComponent component, AfterInteractEvent args)
        {
            if (component.IsSpraying || args.Target is not { Valid: true } target || !args.CanReach)
                return;

            if (!EntityManager.TryGetComponent<PaintableAirlockComponent>(target, out var airlock))
                return;

            string? sprite = GetAirlockSpritePath(airlock.Group, component.Style);
            if (sprite == null)
            {
                string msg = Loc.GetString("airlock-painter-style-not-available");
                _popupSystem.PopupEntity(msg, args.User, Filter.Entities(args.User));
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
                BroadcastFinishedEvent = new AirlockPainterDoAfterComplete(uid, target, sprite, component),
                BroadcastCancelledEvent = new AirlockPainterDoAfterCancelled(component),
            };
            _doAfterSystem.DoAfter(doAfterEventArgs);
        }

        private sealed class AirlockPainterDoAfterComplete : EntityEventArgs
        {
            public readonly EntityUid User;
            public readonly EntityUid Target;
            public readonly string Sprite;
            public readonly AirlockPainterComponent Component;

            public AirlockPainterDoAfterComplete(EntityUid user, EntityUid target, string sprite, AirlockPainterComponent component)
            {
                User = user;
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
            component.Style = (AirlockStyle)args.Index;
        }

        private static string? GetAirlockSpritePath(AirlockGroup grp, AirlockStyle style)
        {
            // This should probably be put into a data file
            switch (grp)
            {
                case AirlockGroup.Standard:
                    switch (style)
                    {
                        case AirlockStyle.Basic:
                            return "Structures/Doors/Airlocks/Standard/basic.rsi";
                        case AirlockStyle.Cargo:
                            return "Structures/Doors/Airlocks/Standard/cargo.rsi";
                        case AirlockStyle.Command:
                            return "Structures/Doors/Airlocks/Standard/command.rsi";
                        case AirlockStyle.Engineering:
                            return "Structures/Doors/Airlocks/Standard/engineering.rsi";
                        case AirlockStyle.External:
                            return "Structures/Doors/Airlocks/Standard/external.rsi";
                        case AirlockStyle.Firelock:
                            return "Structures/Doors/Airlocks/Standard/firelock.rsi";
                        case AirlockStyle.Freezer:
                            return "Structures/Doors/Airlocks/Standard/freezer.rsi";
                        case AirlockStyle.Maintenance:
                            return "Structures/Doors/Airlocks/Standard/maint.rsi";
                        case AirlockStyle.Medical:
                            return "Structures/Doors/Airlocks/Standard/medical.rsi";
                        case AirlockStyle.Science:
                            return "Structures/Doors/Airlocks/Standard/science.rsi";
                        case AirlockStyle.Security:
                            return "Structures/Doors/Airlocks/Standard/security.rsi";
                        case AirlockStyle.Shuttle:
                            return "Structures/Doors/Airlocks/Standard/shuttle.rsi";
                        default:
                            break;
                    }
                    break;
                case AirlockGroup.Glass:
                    switch (style)
                    {
                        case AirlockStyle.Basic:
                            return "Structures/Doors/Airlocks/Glass/basic.rsi";
                        case AirlockStyle.Cargo:
                            return "Structures/Doors/Airlocks/Glass/cargo.rsi";
                        case AirlockStyle.Command:
                            return "Structures/Doors/Airlocks/Glass/command.rsi";
                        case AirlockStyle.Engineering:
                            return "Structures/Doors/Airlocks/Glass/engineering.rsi";
                        case AirlockStyle.External:
                            return "Structures/Doors/Airlocks/Glass/external.rsi";
                        case AirlockStyle.Firelock:
                            return "Structures/Doors/Airlocks/Glass/firelock.rsi";
                        case AirlockStyle.Freezer:
                            return null;
                        case AirlockStyle.Maintenance:
                            return null;
                        case AirlockStyle.Medical:
                            return "Structures/Doors/Airlocks/Glass/medical.rsi";
                        case AirlockStyle.Science:
                            return "Structures/Doors/Airlocks/Glass/science.rsi";
                        case AirlockStyle.Security:
                            return "Structures/Doors/Airlocks/Glass/security.rsi";
                        case AirlockStyle.Shuttle:
                            return "Structures/Doors/Airlocks/Glass/shuttle.rsi";
                        default:
                            break;
                    }
                    break;
            }
            return null;
        }
    }
}
