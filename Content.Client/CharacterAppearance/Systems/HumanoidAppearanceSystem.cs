using Content.Client.Cuffs.Components;
using Content.Shared.Body.Components;
using Content.Shared.CharacterAppearance;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.CharacterAppearance.Systems;
using Content.Shared.Preferences;
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
            SubscribeLocalEvent<HumanoidAppearanceComponent, ComponentInit>(OnHumanoidAppearanceInit);
            SubscribeLocalEvent<ChangedHumanoidAppearanceEvent>(OnAppearanceChange);
            SubscribeNetworkEvent<ChangedHumanoidAppearanceEvent>(OnAppearanceChange);
            SubscribeNetworkEvent<HumanoidAppearanceBodyPartAddedEvent>(BodyPartAdded);
            SubscribeNetworkEvent<HumanoidAppearanceBodyPartRemovedEvent>(BodyPartRemoved);
        }

        private void OnHumanoidAppearanceInit(EntityUid uid, HumanoidAppearanceComponent component, ComponentInit _)
        {
            RaiseNetworkEvent(new HumanoidAppearanceComponentInitEvent(uid));
        }

        public override void UpdateFromProfile(EntityUid uid, ICharacterProfile profile)
        {
            if (!EntityManager.TryGetEntity(uid, out var entity)) return;
            if (!entity.HasComponent<HumanoidAppearanceComponent>()) return;

            var humanoid = (HumanoidCharacterProfile) profile;
            var appearanceChangeEvent = new ChangedHumanoidAppearanceEvent(uid, humanoid);
            RaiseLocalEvent(appearanceChangeEvent);
        }

        public override void OnAppearanceChange(ChangedHumanoidAppearanceEvent args)
        {
            if (!EntityManager.TryGetEntity(args.Uid, out var entity)) return;
            if (!entity.TryGetComponent(out HumanoidAppearanceComponent? component))
                return;

            component.Appearance = args.Appearance;
            component.Sex = args.Sex;
            component.Gender = args.Gender;
            UpdateLooks(args.Uid, component);
        }

        private void UpdateLooks(EntityUid uid, HumanoidAppearanceComponent component)
        {
            if(!EntityManager.TryGetEntity(uid, out var owner)) return;
            if (!owner.TryGetComponent(out SpriteComponent? sprite)) return;

            if (owner.TryGetComponent(out SharedBodyComponent? body))
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

            sprite.LayerSetColor(HumanoidVisualLayers.Eyes, component.Appearance.EyeColor);

            sprite.LayerSetState(HumanoidVisualLayers.Chest, component.Sex == Sex.Male ? "torso_m" : "torso_f");
            sprite.LayerSetState(HumanoidVisualLayers.Head, component.Sex == Sex.Male ? "head_m" : "head_f");

            if (sprite.LayerMapTryGet(HumanoidVisualLayers.StencilMask, out _))
                sprite.LayerSetVisible(HumanoidVisualLayers.StencilMask, component.Sex == Sex.Female);

            if (owner.TryGetComponent<CuffableComponent>(out var cuffed))
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
