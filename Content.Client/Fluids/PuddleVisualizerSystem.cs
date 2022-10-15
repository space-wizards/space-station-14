using System.Linq;
using Content.Shared.Fluids;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Random;

namespace Content.Client.Fluids
{
    [UsedImplicitly]
    public sealed class PuddleVisualizerSystem : VisualizerSystem<PuddleVisualizerComponent>
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PuddleVisualizerComponent, ComponentInit>(OnComponentInit);
        }

        private void OnComponentInit(EntityUid uid, PuddleVisualizerComponent puddleVisuals, ComponentInit args)
        {
            if (!TryComp(uid, out AppearanceComponent? appearance))
            {
                return;
            }

            if (!TryComp(uid, out SpriteComponent? sprite))
            {
                return;
            }

            puddleVisuals.OriginalRsi = sprite.BaseRSI; //Back up the original RSI upon initialization
            RandomizeState(sprite, puddleVisuals.OriginalRsi);
            RandomizeRotation(sprite);
        }

        protected override void OnAppearanceChange(EntityUid uid, PuddleVisualizerComponent component, ref AppearanceChangeEvent args)
        {
            if (!TryComp(uid, out SpriteComponent? sprite))
            {
                Logger.Warning($"Missing SpriteComponent for PuddleVisualizerSystem on entityUid = {uid}");
                return;
            }

            if (!args.Component.TryGetData(PuddleVisuals.VolumeScale, out float volumeScale)
                || !args.Component.TryGetData(PuddleVisuals.CurrentVolume, out FixedPoint2 currentVolume)
                || !args.Component.TryGetData(PuddleVisuals.SolutionColor, out Color solutionColor)
                || !args.Component.TryGetData(PuddleVisuals.IsSlipperyVisual, out bool isSlippery)
                || !args.Component.TryGetData(PuddleVisuals.IsEvaporatingVisual, out bool isEvaporating))
            {
                if (!component.CustomPuddleSprite) //if there is a custom sprite, it's expected that some of these will be missing, so suppress the logger (no need to warn for it)
                {
                    Logger.Warning($"Missing PuddleVisuals data for PuddleVisualizerSystem on entityUid = {uid}");
                }
                return; //regardless of custom sprite
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

            sprite.LayerSetColor(0, newColor);

            if (!component.CustomPuddleSprite) //Don't consider wet floor effects if we're using a custom sprite.
            {
                bool wetFloorEffectNeeded;

                if (/*isSlippery
                    && */isEvaporating
                    && currentVolume < component.WetFloorEffectThreshold)
                {
                    wetFloorEffectNeeded = true;
                }
                else
                {
                    wetFloorEffectNeeded = false;
                }

                if (wetFloorEffectNeeded)
                {
                    if (sprite.LayerGetState(0) != "sparkles") // If we need the effect but don't already have it - start it
                    {
                        StartWetFloorEffect(sprite, component.WetFloorEffectAlpha);
                    }
                }
                else
                {
                    if (sprite.LayerGetState(0) == "sparkles") // If we have the effect but don't need it - end it
                        EndWetFloorEffect(sprite, component.OriginalRsi);
                }
            }
        }

        private void StartWetFloorEffect(SpriteComponent sprite, float alpha)
        {
            sprite.LayerSetState(0, "sparkles", "Fluids/wet_floor_sparkles.rsi");
            sprite.Color = sprite.Color.WithAlpha(alpha);
            sprite.LayerSetAutoAnimated(0, true);
        }

        private void EndWetFloorEffect(SpriteComponent sprite, RSI? originalRSI)
        {
            RandomizeState(sprite, originalRSI);
            sprite.LayerSetAutoAnimated(0, false);
        }

        private void RandomizeState(SpriteComponent sprite, RSI? rsi)
        {
            var maxStates = rsi?.ToArray();
            if (maxStates is not { Length: > 0 }) return;

            var selectedState = _random.Next(0, maxStates.Length - 1); //randomly select an index for which RSI state to use.
            sprite.LayerSetState(0, maxStates[selectedState].StateId, rsi); // sets the sprite's state via our randomly selected index.
        }

        private void RandomizeRotation(SpriteComponent sprite)
        {
            float rotationDegrees = _random.Next(0, 359); // randomly select a rotation for our puddle sprite.
            sprite.Rotation = Angle.FromDegrees(rotationDegrees); // sets the sprite's rotation to the one we randomly selected.
        }
    }
}
