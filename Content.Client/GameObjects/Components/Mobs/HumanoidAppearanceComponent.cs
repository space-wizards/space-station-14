using Content.Client.GameObjects.Components.ActionBlocking;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Appearance;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Mobs
{
    [RegisterComponent]
    public sealed class HumanoidAppearanceComponent : SharedHumanoidAppearanceComponent, IBodyPartAdded, IBodyPartRemoved
    {
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
            if (Appearance is null ||
                !Owner.TryGetComponent(out SpriteComponent? sprite))
            {
                return;
            }

            if (Owner.TryGetComponent(out IBody? body))
            {
                foreach (var part in body.Parts.Values)
                {
                    if (!part.Owner.TryGetComponent(out SpriteComponent? partSprite))
                    {
                        continue;
                    }

                    partSprite.Color = Appearance.SkinColor;
                }
            }

            sprite.LayerSetColor(HumanoidVisualLayers.Hair, Appearance.HairColor);
            sprite.LayerSetColor(HumanoidVisualLayers.FacialHair, Appearance.FacialHairColor);

            sprite.LayerSetColor(HumanoidVisualLayers.Eyes, Appearance.EyeColor);

            sprite.LayerSetState(HumanoidVisualLayers.Chest, Sex == Sex.Male ? "torso_m" : "torso_f");
            sprite.LayerSetState(HumanoidVisualLayers.Head, Sex == Sex.Male ? "head_m" : "head_f");

            sprite.LayerSetVisible(HumanoidVisualLayers.StencilMask, Sex == Sex.Female);

            if (Owner.TryGetComponent<CuffableComponent>(out var cuffed))
            {
                sprite.LayerSetVisible(HumanoidVisualLayers.Handcuffs, !cuffed.CanStillInteract);
            }
            else
            {
                sprite.LayerSetVisible(HumanoidVisualLayers.Handcuffs, false);
            }

            var hairStyle = Appearance.HairStyleName;
            if (string.IsNullOrWhiteSpace(hairStyle) || !HairStyles.HairStylesMap.ContainsKey(hairStyle))
                hairStyle = HairStyles.DefaultHairStyle;
            sprite.LayerSetState(HumanoidVisualLayers.Hair,
                HairStyles.HairStylesMap[hairStyle]);

            var facialHairStyle = Appearance.FacialHairStyleName;
            if (string.IsNullOrWhiteSpace(facialHairStyle) || !HairStyles.FacialHairStylesMap.ContainsKey(facialHairStyle))
                facialHairStyle = HairStyles.DefaultFacialHairStyle;
            sprite.LayerSetState(HumanoidVisualLayers.FacialHair,
                HairStyles.FacialHairStylesMap[facialHairStyle]);
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
