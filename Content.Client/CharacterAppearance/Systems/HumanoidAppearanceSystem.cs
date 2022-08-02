using Content.Client.Body.Components;
using Content.Client.Body.Systems;
using Content.Client.Cuffs.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
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
        [Dependency] private readonly BodySystem _bodySystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HumanoidAppearanceComponent, ChangedHumanoidAppearanceEvent>(UpdateLooks);
            SubscribeLocalEvent<HumanoidAppearanceComponent, PartAddedToBodyEvent>(BodyPartAdded);
            SubscribeLocalEvent<HumanoidAppearanceComponent, PartRemovedFromBodyEvent>(BodyPartRemoved);
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
                foreach (var part in _bodySystem.GetAllParts(uid, body))
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

        private void UpdateBodyPartVisuals(EntityUid uid)
        {
            if (!TryComp<SpriteComponent>(uid, out var sprite))
                return;

            if (!TryComp<BodyComponent>(uid, out var body))
                return;

            var humanoidLayers = new HashSet<HumanoidVisualLayers>();
            foreach (var part in _bodySystem.GetAllParts(uid, body))
            {
                if (!HasComp<SpriteComponent>(part.Owner))
                    continue;

                foreach (var hlayer in part.ToHumanoidLayers())
                {
                    humanoidLayers.Add(hlayer);
                }
            }

            foreach (var layer in BodyPartLayers)
            {
                if (!sprite.LayerMapTryGet(layer, out _))
                    continue;

                var visible = humanoidLayers.Contains(layer);
                sprite.LayerSetVisible(layer, visible);
            }
        }

        private void BodyPartAdded(EntityUid uid, HumanoidAppearanceComponent component, PartAddedToBodyEvent args)
        {
            UpdateBodyPartVisuals(uid);
        }

        private void BodyPartRemoved(EntityUid uid, HumanoidAppearanceComponent component, PartRemovedFromBodyEvent args)
        {
            UpdateBodyPartVisuals(uid);
        }
    }
}
