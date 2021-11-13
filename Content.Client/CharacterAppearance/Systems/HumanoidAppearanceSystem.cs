using System.Collections.Generic;
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

namespace Content.Client.CharacterAppearance.Systems
{
    public class HumanoidAppearanceSystem : SharedHumanoidAppearanceSystem
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

        private List<HumanoidVisualLayers> _bodyPartLayers = new List<HumanoidVisualLayers>
        {
            HumanoidVisualLayers.Chest,
            HumanoidVisualLayers.Head,
            HumanoidVisualLayers.Eyes,
            HumanoidVisualLayers.RArm,
            HumanoidVisualLayers.LArm,
            HumanoidVisualLayers.RHand,
            HumanoidVisualLayers.LHand,
            HumanoidVisualLayers.RLeg,
            HumanoidVisualLayers.LLeg,
        };

        private void UpdateLooks(EntityUid uid, HumanoidAppearanceComponent component, ChangedHumanoidAppearanceEvent args)
        {
            if(!EntityManager.TryGetComponent(uid, out SpriteComponent? sprite))
                return;

            if (EntityManager.TryGetComponent(uid, out SharedBodyComponent? body))
            {
                foreach (var (part, _) in body.Parts)
                {
                    if (part.Owner.TryGetComponent(out SpriteComponent? partSprite))
                    {
                        partSprite!.Color = component.Appearance.SkinColor;
                    }

                }
            }

            sprite.LayerSetColor(HumanoidVisualLayers.Hair,
                component.CanColorHair ? component.Appearance.HairColor : Color.White);
            sprite.LayerSetColor(HumanoidVisualLayers.FacialHair,
                component.CanColorFacialHair ? component.Appearance.FacialHairColor : Color.White);

            foreach (var layer in _bodyPartLayers)
                sprite.LayerSetColor(layer, component.Appearance.SkinColor);

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

            var hairStyle = component.Appearance.HairStyleId;
            if (string.IsNullOrWhiteSpace(hairStyle) ||
                !_accessoryManager.IsValidAccessoryInCategory(hairStyle, component.CategoriesHair))
            {
                hairStyle = HairStyles.DefaultHairStyle;
            }

            var facialHairStyle = component.Appearance.FacialHairStyleId;
            if (string.IsNullOrWhiteSpace(facialHairStyle) ||
                !_accessoryManager.IsValidAccessoryInCategory(facialHairStyle, component.CategoriesFacialHair))
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
            {
                if (!sprite.LayerMapTryGet(layer, out _))
                    continue;

                sprite.LayerSetVisible(layer, true);
            }
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
