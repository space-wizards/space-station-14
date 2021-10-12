using Content.Client.Cuffs.Components;
using Content.Shared.Body.Components;
using Content.Shared.CharacterAppearance;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.CharacterAppearance.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.CharacterAppearance
{
    public class HumanoidAppearanceSystem : SharedHumanoidAppearanceSystem
    {
        [Dependency] private readonly SpriteAccessoryManager _accessoryManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<HumanoidAppearanceProfileChangedEvent>(UpdateLooks);
            SubscribeNetworkEvent<HumanoidAppearanceProfileChangedEvent>(UpdateLooks);
            SubscribeNetworkEvent<HumanoidAppearanceBodyPartAddedEvent>(BodyPartAdded);
            SubscribeNetworkEvent<HumanoidAppearanceBodyPartRemovedEvent>(BodyPartRemoved);
        }

        public void UpdateLooks(HumanoidAppearanceProfileChangedEvent args)
        {
            if(!EntityManager.TryGetEntity(args.Uid, out var owner)) return;
            var characterProfile = args.Profile;

            if (!owner.TryGetComponent(out SpriteComponent? sprite)
                || !owner.TryGetComponent(out HumanoidAppearanceComponent? profile))
                return;

            if (owner.TryGetComponent(out SharedBodyComponent? body))
            {
                foreach (var (part, _) in body.Parts)
                {
                    if (part.Owner.TryGetComponent(out SpriteComponent? partSprite))
                    {
                        partSprite!.Color = characterProfile.Appearance.SkinColor;
                    }

                }
            }

            sprite.LayerSetColor(HumanoidVisualLayers.Hair,
                profile.CanColorHair ? characterProfile.Appearance.HairColor : Color.White);
            sprite.LayerSetColor(HumanoidVisualLayers.FacialHair,
                profile.CanColorFacialHair ? characterProfile.Appearance.FacialHairColor : Color.White);

            sprite.LayerSetColor(HumanoidVisualLayers.Eyes, characterProfile.Appearance.EyeColor);

            sprite.LayerSetState(HumanoidVisualLayers.Chest, profile.Sex == Sex.Male ? "torso_m" : "torso_f");
            sprite.LayerSetState(HumanoidVisualLayers.Head, profile.Sex == Sex.Male ? "head_m" : "head_f");

            if (sprite.LayerMapTryGet(HumanoidVisualLayers.StencilMask, out _))
                sprite.LayerSetVisible(HumanoidVisualLayers.StencilMask, profile.Sex == Sex.Female);

            if (owner.TryGetComponent<CuffableComponent>(out var cuffed))
            {
                sprite.LayerSetVisible(HumanoidVisualLayers.Handcuffs, !cuffed.CanStillInteract);
            }
            else
            {
                sprite.LayerSetVisible(HumanoidVisualLayers.Handcuffs, false);
            }

            var hairStyle = characterProfile.Appearance.HairStyleId;
            if (string.IsNullOrWhiteSpace(hairStyle) ||
                !_accessoryManager.IsValidAccessoryInCategory(hairStyle, profile.CategoriesHair))
            {
                hairStyle = HairStyles.DefaultHairStyle;
            }

            var facialHairStyle = characterProfile.Appearance.FacialHairStyleId;
            if (string.IsNullOrWhiteSpace(facialHairStyle) ||
                !_accessoryManager.IsValidAccessoryInCategory(facialHairStyle, profile.CategoriesFacialHair))
            {
                facialHairStyle = HairStyles.DefaultFacialHairStyle;
            }

            var hairPrototype = _prototypeManager.Index<SpriteAccessoryPrototype>(hairStyle);
            var facialHairPrototype = _prototypeManager.Index<SpriteAccessoryPrototype>(facialHairStyle);

            sprite.LayerSetSprite(HumanoidVisualLayers.Hair, hairPrototype.Sprite);
            sprite.LayerSetSprite(HumanoidVisualLayers.FacialHair, facialHairPrototype.Sprite);
        }

        // Scaffolding until Body is moved to ECS.
        private void BodyPartAdded(HumanoidAppearanceBodyPartAddedEvent args)
        {
            if(!EntityManager.TryGetEntity(args.Uid, out var owner)) return;
            if (!owner.TryGetComponent(out SpriteComponent? sprite))
            {
                return;
            }

            if (!args.Args.Part.Owner.HasComponent<SpriteComponent>())
            {
                return;
            }

            var layers = args.Args.Part.ToHumanoidLayers();
            // TODO BODY Layer color, sprite and state
            foreach (var layer in layers)
                sprite.LayerSetVisible(layer, true);
        }

        private void BodyPartRemoved(HumanoidAppearanceBodyPartRemovedEvent args)
        {
            if(!EntityManager.TryGetEntity(args.Uid, out var owner)) return;
            if (!owner.TryGetComponent(out SpriteComponent? sprite))
            {
                return;
            }

            if (!args.Args.Part.Owner.HasComponent<SpriteComponent>())
            {
                return;
            }

            var layers = args.Args.Part.ToHumanoidLayers();
            // TODO BODY Layer color, sprite and state
            foreach (var layer in layers)
                sprite.LayerSetVisible(layer, false);

        }
    }
}
