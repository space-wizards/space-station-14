using System.Linq;
using Content.Shared.Fluids;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Random;

namespace Content.Client.Fluids
{
    [UsedImplicitly]
    public sealed class PuddleVisualsSystem : VisualizerSystem<PuddleVisualsComponent>
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PuddleVisualsComponent, ComponentInit>(OnComponentInit);
        }

        private void OnComponentInit(EntityUid uid, PuddleVisualsComponent puddleVisuals, ComponentInit args)
        {
            if (!TryComp(uid, out AppearanceComponent? appearance))
            {
                return;
            }

            if (!TryComp(uid, out SpriteComponent? sprite))
            {
                return;
            }

            if (!appearance.TryGetData(PuddleVisuals.VisualSeed, out float visualSeed))
            {
                return;
            }

            var maxStates = sprite.BaseRSI?.ToArray();

            if (maxStates is not { Length: > 0 }) return;

            int selectedState = (int) Math.Floor(visualSeed * maxStates.Length); // uses the visualSeed to randomly select an index for which RSI state to use.
            sprite.LayerSetState(PuddleVisualLayers.Puddle, maxStates[selectedState].StateId); // sets the sprite's state via our randomly selected index.

            float rotationDegrees = (visualSeed * maxStates.Length - selectedState) * 360; // uses the visualSeed to randomly select a rotation for our puddle sprite.
            sprite.Rotation = Angle.FromDegrees(rotationDegrees); // sets the sprite's rotation to the one we randomly selected.
        }


        protected override void OnAppearanceChange(EntityUid uid, PuddleVisualsComponent component, ref AppearanceChangeEvent args)
        {
            if (!TryComp(uid, out SpriteComponent? sprite)
                || !args.Component.TryGetData(PuddleVisuals.VolumeScale, out float volumeScale)
                || !args.Component.TryGetData(PuddleVisuals.SolutionColor, out Color solutionColor)
                || !args.Component.TryGetData(PuddleVisuals.WetFloorEffect, out bool wetFloorEffect)
                )
            {
                Logger.Warning($"Missing SpriteComponent for PuddleVisualsSystem on entityUid = {uid}");
                return;
            }

            // volumeScale is our opacity based on level of fullness to overflow. The lower bound is hard-capped for visibility reasons.
            var cappedScale = Math.Min(1.0f, volumeScale * 0.75f + 0.25f);

            Color newColor;
            if (component.Recolor)
            {
                newColor = solutionColor.WithAlpha(cappedScale);
            }
            else
            {
                newColor = sprite.Color.WithAlpha(cappedScale);
            }

            if (!component.Sparkly)
                return;

            if (wetFloorEffect)
            {
                // Hides the main puddle sprite layer
                sprite.LayerSetVisible(PuddleVisualLayers.Puddle, false);

                // Shows the wet floor sprite layers
                sprite.LayerSetState(PuddleVisualLayers.WetFloorEffect, component.EffectState, component.EffectRsi);
                sprite.LayerSetColor(PuddleVisualLayers.WetFloorEffect, newColor.WithAlpha(0.25f)); //Sparkles inherit the color of the puddle's solution, except they should be mostly transparent.
                sprite.LayerSetVisible(PuddleVisualLayers.WetFloorEffect, true);

            }
            else
            {
                // Hides the wet floor sprite layer
                sprite.LayerSetVisible(PuddleVisualLayers.WetFloorEffect, false);

                // Shows the main puddle sprite layer
                sprite.LayerSetColor(PuddleVisualLayers.Puddle, newColor);
                sprite.LayerSetVisible(PuddleVisualLayers.Puddle, true);
            }
        }
    }
}

public enum PuddleVisualLayers
{
    Puddle,
    WetFloorEffect
}
