using Content.Client.Cuffs.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.CharacterAppearance;
using Content.Shared.CharacterAppearance.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.CharacterAppearance
{
    [RegisterComponent]
    public sealed class HumanoidAppearanceComponent : SharedHumanoidAppearanceComponent, IBodyPartAdded, IBodyPartRemoved
    {
        [Dependency] private readonly SpriteAccessoryManager _accessoryManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override HumanoidCharacterAppearance Appearance
        {
            get => base.Appearance;
            set
            {
                base.Appearance = value;
                UpdateLooks();
            }
        }

        public override Sex Sex
        {
            get => base.Sex;
            set
            {
                base.Sex = value;
                UpdateLooks();
            }
        }

        protected override void Startup()
        {
            base.Startup();

            UpdateLooks();
        }

        private void UpdateLooks()
        {
            if (Appearance is null! ||
                !Owner.TryGetComponent(out SpriteComponent? sprite))
            {
                return;
            }

            if (Owner.TryGetComponent(out IBody? body))
            {
                foreach (var (part, _) in body.Parts)
                {
                    if (!part.Owner.TryGetComponent(out SpriteComponent? partSprite))
                    {
                        continue;
                    }

                    partSprite.Color = Appearance.SkinColor;
                }
            }

            sprite.LayerSetColor(HumanoidVisualLayers.Hair,
                CanColorHair ? Appearance.HairColor : Color.White);
            sprite.LayerSetColor(HumanoidVisualLayers.FacialHair,
                CanColorFacialHair ? Appearance.FacialHairColor : Color.White);

            sprite.LayerSetColor(HumanoidVisualLayers.Eyes, Appearance.EyeColor);

            sprite.LayerSetState(HumanoidVisualLayers.Chest, Sex == Sex.Male ? "torso_m" : "torso_f");
            sprite.LayerSetState(HumanoidVisualLayers.Head, Sex == Sex.Male ? "head_m" : "head_f");

            if (sprite.LayerMapTryGet(HumanoidVisualLayers.StencilMask, out _))
                sprite.LayerSetVisible(HumanoidVisualLayers.StencilMask, Sex == Sex.Female);

            if (Owner.TryGetComponent<CuffableComponent>(out var cuffed))
            {
                sprite.LayerSetVisible(HumanoidVisualLayers.Handcuffs, !cuffed.CanStillInteract);
            }
            else
            {
                sprite.LayerSetVisible(HumanoidVisualLayers.Handcuffs, false);
            }

            var hairStyle = Appearance.HairStyleId;
            if (string.IsNullOrWhiteSpace(hairStyle) ||
                !_accessoryManager.IsValidAccessoryInCategory(hairStyle, CategoriesHair))
            {
                hairStyle = HairStyles.DefaultHairStyle;
            }

            var facialHairStyle = Appearance.FacialHairStyleId;
            if (string.IsNullOrWhiteSpace(facialHairStyle) ||
                !_accessoryManager.IsValidAccessoryInCategory(facialHairStyle, CategoriesFacialHair))
            {
                facialHairStyle = HairStyles.DefaultFacialHairStyle;
            }

            var hairPrototype = _prototypeManager.Index<SpriteAccessoryPrototype>(hairStyle);
            var facialHairPrototype = _prototypeManager.Index<SpriteAccessoryPrototype>(facialHairStyle);

            sprite.LayerSetSprite(HumanoidVisualLayers.Hair, hairPrototype.Sprite);
            sprite.LayerSetSprite(HumanoidVisualLayers.FacialHair, facialHairPrototype.Sprite);
        }

        public void BodyPartAdded(BodyPartAddedEventArgs args)
        {
            if (!Owner.TryGetComponent(out SpriteComponent? sprite))
            {
                return;
            }

            if (!args.Part.Owner.TryGetComponent(out SpriteComponent? partSprite))
            {
                return;
            }

            var layer = args.Part.ToHumanoidLayer();

            if (layer == null)
            {
                return;
            }

            // TODO BODY Layer color, sprite and state
            sprite.LayerSetVisible(layer, true);
        }

        public void BodyPartRemoved(BodyPartRemovedEventArgs args)
        {
            if (!Owner.TryGetComponent(out SpriteComponent? sprite))
            {
                return;
            }

            if (!args.Part.Owner.TryGetComponent(out SpriteComponent? partSprite))
            {
                return;
            }

            var layer = args.Part.ToHumanoidLayer();

            if (layer == null)
            {
                return;
            }

            // TODO BODY Layer color, sprite and state
            sprite.LayerSetVisible(layer, false);
        }
    }
}
