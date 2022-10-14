using System.Linq;
using Content.Shared.Fluids;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
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

            // if (!appearance.TryGetData(PuddleVisuals.VisualSeed, out float visualSeed))
            // {
            //     return;
            // }

            var maxStates = sprite.BaseRSI?.ToArray();

            if (maxStates is not { Length: > 0 }) return;

            RandomizeState(sprite, maxStates);
            RandomizeRotation(sprite);
        }


        protected override void OnAppearanceChange(EntityUid uid, PuddleVisualizerComponent component, ref AppearanceChangeEvent args)
        {
            bool wetFloorEffectNeeded;

            if (!TryComp(uid, out SpriteComponent? sprite)
                || !args.Component.TryGetData(PuddleVisuals.VolumeScale, out float volumeScale)
                || !args.Component.TryGetData(PuddleVisuals.CurrentVolume, out FixedPoint2 currentVolume)
                || !args.Component.TryGetData(PuddleVisuals.SolutionColor, out Color solutionColor)
                || !args.Component.TryGetData(PuddleVisuals.IsSlipperyVisual, out bool isSlippery)
                || !args.Component.TryGetData(PuddleVisuals.IsEvaporatingVisual, out bool isEvaporating)
                )
            {
                Logger.Warning($"Missing SpriteComponent for PuddleVisualizerSystem on entityUid = {uid}");
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

            sprite.LayerSetColor(0, newColor);

            if (component.CustomPuddleSprite) //Don't consider wet floor effects if we're using a custom sprite.
                return;

            if (/*isSlippery
                && */ isEvaporating
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
                if (sprite.LayerGetState(0) != "sparkles")
                {
                    StartWetFloorEffect(sprite);
                }
            }
            else
            {
                if (sprite.LayerGetState(0) == "sparkles")
                    EndWetFloorEffect(sprite);
            }
        }

        private void StartWetFloorEffect(SpriteComponent sprite)
        {
            sprite.LayerSetState(0, "sparkles", "Fluids/wet_floor_sparkles.rsi");
            sprite.Color = sprite.Color.WithAlpha(1.00f); //should be mostly transparent.
            sprite.LayerSetAutoAnimated(0, true);
        }

        private void EndWetFloorEffect(SpriteComponent sprite)
        {
            sprite.LayerSetState(0, "smear-0", "Fluids/smear.rsi"); // TODO: need a way to implement the random smears again when the mop creates new puddles.
            sprite.LayerSetAutoAnimated(0, false);
        }

        private void RandomizeState(SpriteComponent sprite, Robust.Client.Graphics.RSI.State[] maxStates)
        {
            var selectedState = _random.Next(0, maxStates.Length - 1); //randomly select an index for which RSI state to use.
            sprite.LayerSetState(0, maxStates[selectedState].StateId); // sets the sprite's state via our randomly selected index.
        }

        private void RandomizeRotation(SpriteComponent sprite)
        {
            float rotationDegrees = _random.Next(0, 359); // randomly select a rotation for our puddle sprite.
            sprite.Rotation = Angle.FromDegrees(rotationDegrees); // sets the sprite's rotation to the one we randomly selected.
        }
    }
}
