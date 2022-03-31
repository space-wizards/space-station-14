using Content.Server.CharacterAppearance.Systems;
using Content.Server.UserInterface;
using Content.Shared.CharacterAppearance;
using Content.Shared.CharacterAppearance.Components;
using Robust.Server.GameObjects;

namespace Content.Server.CharacterAppearance.Components
{
    [RegisterComponent]
    public sealed class MagicMirrorComponent : SharedMagicMirrorComponent
    {
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly SpriteAccessoryManager _spriteAccessoryManager = default!;

        [ViewVariables] public BoundUserInterface? UserInterface => Owner.GetUIOrNull(MagicMirrorUiKey.Key);

        protected override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }
        }

        protected override void OnRemove()
        {
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage -= OnUiReceiveMessage;
            }

            base.OnRemove();
        }

        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Session.AttachedEntity is not {Valid: true} player)
            {
                return;
            }

            if (!_entities.TryGetComponent(player, out HumanoidAppearanceComponent? looks))
            {
                return;
            }

            switch (obj.Message)
            {
                case HairSelectedMessage msg:
                    var cat = msg.IsFacialHair
                        ? looks.CategoriesFacialHair
                        : looks.CategoriesHair;

                    if (!_spriteAccessoryManager.IsValidAccessoryInCategory(msg.HairId, cat))
                        return;

                    looks.Appearance = msg.IsFacialHair
                        ? looks.Appearance.WithFacialHairStyleName(msg.HairId)
                        : looks.Appearance.WithHairStyleName(msg.HairId);

                    break;

                case HairColorSelectedMessage msg:
                    if (msg.IsFacialHair ? !looks.CanColorFacialHair : !looks.CanColorHair)
                        return;

                    var (r, g, b) = msg.HairColor;
                    var color = new Color(r, g, b);

                    looks.Appearance = msg.IsFacialHair
                        ? looks.Appearance.WithFacialHairColor(color)
                        : looks.Appearance.WithHairColor(color);

                    break;

                case EyeColorSelectedMessage msg:
                    var (eyeR, eyeG, eyeB) = msg.EyeColor;
                    var eyeColor = new Color(eyeR, eyeG, eyeB);

                    looks.Appearance = looks.Appearance.WithEyeColor(eyeColor);

                    break;
            }

            EntitySystem.Get<HumanoidAppearanceSystem>().ForceAppearanceUpdate(player);
        }
    }
}
