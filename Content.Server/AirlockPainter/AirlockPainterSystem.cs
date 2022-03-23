using Content.Server.UserInterface;
using Content.Shared.AirlockPainter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Server.AirlockPainter
{
    /// <summary>
    /// A system for painting airlocks using airlock painter
    /// </summary>
    [UsedImplicitly]
    public sealed class AirlockPainterSystem : EntitySystem
    {

        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<AirlockPainterComponent, AfterInteractEvent>(AfterInteractOn);
            SubscribeLocalEvent<AirlockPainterComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<AirlockPainterComponent, AirlockPainterSpritePickedMessage>(OnSpritePicked);
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
            if (args.Target is not { Valid: true } target || !args.CanReach || !component.Whitelist.IsValid(target))
                return;

            SetSprite(uid, component, target, out string? result);
            if (result != null)
                component.Owner.PopupMessage(args.User, result);
        }

        private void SetSprite(EntityUid uid, AirlockPainterComponent? component, EntityUid target, out string? result)
        {
            if (!Resolve(uid, ref component))
            {
                result = null;
                return;
            }

            SpriteComponent sprite = target.EnsureComponent<SpriteComponent>();
            sprite.BaseRSIPath = component.SpriteList[component.Index];
            result = Loc.GetString("sprite-successfully-changed");
        }

        private void OnSpritePicked(EntityUid uid, AirlockPainterComponent component, AirlockPainterSpritePickedMessage args)
        {
            component.Index = args.Index;
            DirtyUI(uid, component);
        }

        private void DirtyUI(EntityUid uid, AirlockPainterComponent? component = null)
        {
            if (!Resolve(uid, ref component))
            {
                Logger.Debug("AirlockPainterComponent not found");
                return;
            }

            _userInterfaceSystem.TrySetUiState(uid, AirlockPainterUiKey.Key,
                new AirlockPainterBoundUserInterfaceState(component.SpriteList));
        }
    }
}
