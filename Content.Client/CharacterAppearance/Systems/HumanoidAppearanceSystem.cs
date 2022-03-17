using Content.Client.Cuffs.Components;
using Content.Shared.Body.Components;
using Content.Shared.CharacterAppearance;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.CharacterAppearance.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.CharacterAppearance.Systems
{
    public sealed class HumanoidAppearanceSystem : SharedHumanoidAppearanceSystem
    {
        [Dependency] private readonly SpriteAccessoryManager _accessoryManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HumanoidAppearanceComponent, ChangedHumanoidAppearanceEvent>(UpdateLooks);
            SubscribeLocalEvent<HumanoidAppearanceBodyPartAddedEvent>(BodyPartAdded);
            SubscribeLocalEvent<HumanoidAppearanceBodyPartRemovedEvent>(BodyPartRemoved);
        }

        public readonly static HumanoidVisualLayers[] BodyPartLayers = {
            HumanoidVisualLayers.Chest,
            HumanoidVisualLayers.Head,
            HumanoidVisualLayers.Snout,
            HumanoidVisualLayers.HeadTop,
            HumanoidVisualLayers.HeadSide,
            HumanoidVisualLayers.Tail,
            HumanoidVisualLayers.Eyes,
            HumanoidVisualLayers.RArm,
            HumanoidVisualLayers.LArm,
            HumanoidVisualLayers.RHand,
            HumanoidVisualLayers.LHand,
            HumanoidVisualLayers.RLeg,
            HumanoidVisualLayers.LLeg,
            HumanoidVisualLayers.RFoot,
            HumanoidVisualLayers.LFoot
        };

        private void UpdateLooks(EntityUid uid, HumanoidAppearanceComponent component,
            ChangedHumanoidAppearanceEvent args)
        {
            var spriteQuery = EntityManager.GetEntityQuery<SpriteComponent>();

            if (!spriteQuery.TryGetComponent(uid, out var sprite))
                return;

            if (EntityManager.TryGetComponent(uid, out SharedBodyComponent? body))
            {
                foreach (var (part, _) in body.Parts)
                {
                    if (spriteQuery.TryGetComponent(part.Owner, out var partSprite))
                    {
                        partSprite.Color = component.Appearance.SkinColor;
                    }
                }
            }

            // Like body parts some stuff may not have hair.
            if (sprite.LayerMapTryGet(HumanoidVisualLayers.Hair, out var hairLayer))
            {
                var hairColor = component.CanColorHair ? component.Appearance.HairColor : Color.White;
                hairColor = component.HairMatchesSkin ? component.Appearance.SkinColor : hairColor;
                sprite.LayerSetColor(hairLayer, hairColor.WithAlpha(component.HairAlpha));

                var hairStyle = component.Appearance.HairStyleId;
                if (string.IsNullOrWhiteSpace(hairStyle) ||
                    !_accessoryManager.IsValidAccessoryInCategory(hairStyle, component.CategoriesHair))
                {
                    hairStyle = HairStyles.DefaultHairStyle;
                }

                var hairPrototype = _prototypeManager.Index<SpriteAccessoryPrototype>(hairStyle);
                sprite.LayerSetSprite(hairLayer, hairPrototype.Sprite);
            }

            if (sprite.LayerMapTryGet(HumanoidVisualLayers.FacialHair, out var facialLayer))
            {
                var facialHairColor = component.CanColorHair ? component.Appearance.FacialHairColor : Color.White;
                facialHairColor = component.HairMatchesSkin ? component.Appearance.SkinColor : facialHairColor;
                sprite.LayerSetColor(facialLayer, facialHairColor.WithAlpha(component.HairAlpha));

                var facialHairStyle = component.Appearance.FacialHairStyleId;
                if (string.IsNullOrWhiteSpace(facialHairStyle) ||
                    !_accessoryManager.IsValidAccessoryInCategory(facialHairStyle, component.CategoriesFacialHair))
                {
                    facialHairStyle = HairStyles.DefaultFacialHairStyle;
                }

                var facialHairPrototype = _prototypeManager.Index<SpriteAccessoryPrototype>(facialHairStyle);
                sprite.LayerSetSprite(facialLayer, facialHairPrototype.Sprite);
            }

            foreach (var layer in BodyPartLayers)
            {
                // Not every mob may have the furry layers hence we just skip it.
                if (!sprite.LayerMapTryGet(layer, out var actualLayer)) continue;
                if (!sprite[actualLayer].Visible) continue;

                sprite.LayerSetColor(actualLayer, component.Appearance.SkinColor);
            }

            sprite.LayerSetColor(HumanoidVisualLayers.Eyes, component.Appearance.EyeColor);
            sprite.LayerSetState(HumanoidVisualLayers.Chest, component.Sex == Sex.Male ? "torso_m" : "torso_f");
            sprite.LayerSetState(HumanoidVisualLayers.Head, component.Sex == Sex.Male ? "head_m" : "head_f");

            if (sprite.LayerMapTryGet(HumanoidVisualLayers.StencilMask, out _))
                sprite.LayerSetVisible(HumanoidVisualLayers.StencilMask, component.Sex == Sex.Female);

            if (EntityManager.TryGetComponent<CuffableComponent>(uid, out var cuffed))
            {
                sprite.LayerSetVisible(HumanoidVisualLayers.Handcuffs, !cuffed.CanStillInteract);
            }
            else
            {
                sprite.LayerSetVisible(HumanoidVisualLayers.Handcuffs, false);
            }
        }

        // Scaffolding until Body is moved to ECS.
        private void BodyPartAdded(HumanoidAppearanceBodyPartAddedEvent args)
        {
            if (!EntityManager.TryGetComponent(args.Uid, out SpriteComponent? sprite))
            {
                return;
            }

            if (!EntityManager.HasComponent<SpriteComponent>(args.Args.Part.Owner))
            {
                return;
            }

            var layers = args.Args.Part.ToHumanoidLayers();
            // TODO BODY Layer color, sprite and state
            foreach (var layer in layers)
            {
                if (!sprite.LayerMapTryGet(layer, out _))
                    continue;

                sprite.LayerSetVisible(layer, true);
            }
        }

        private void BodyPartRemoved(HumanoidAppearanceBodyPartRemovedEvent args)
        {
            if (!EntityManager.TryGetComponent(args.Uid, out SpriteComponent? sprite))
            {
                return;
            }

            if (!EntityManager.HasComponent<SpriteComponent>(args.Args.Part.Owner))
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
