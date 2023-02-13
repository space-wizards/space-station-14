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
            if (args.Sprite == null)
            {
                Logger.Warning($"Missing SpriteComponent for PuddleVisualizerSystem on entityUid = {uid}");
                return;
            }

            if (!AppearanceSystem.TryGetData<float>(uid, PuddleVisuals.VolumeScale, out var volumeScale)
                || !AppearanceSystem.TryGetData<FixedPoint2>(uid, PuddleVisuals.CurrentVolume, out var currentVolume)
                || !AppearanceSystem.TryGetData<Color>(uid, PuddleVisuals.SolutionColor, out var solutionColor)
                || !AppearanceSystem.TryGetData<bool>(uid, PuddleVisuals.IsEvaporatingVisual, out var isEvaporating))
            {
                return;
            }

            // volumeScale is our opacity based on level of fullness to overflow. The lower bound is hard-capped for visibility reasons.
            var cappedScale = Math.Min(1.0f, volumeScale * 0.75f + 0.25f);

            var newColor = component.Recolor ? solutionColor.WithAlpha(cappedScale) : args.Sprite.Color.WithAlpha(cappedScale);

            args.Sprite.LayerSetColor(0, newColor);

            // Don't consider wet floor effects if we're using a custom sprite.
            if (component.CustomPuddleSprite)
                return;

            if (isEvaporating && currentVolume <= component.WetFloorEffectThreshold)
            {
                // If we need the effect but don't already have it - start it
                if (args.Sprite.LayerGetState(0) != "sparkles")
                    StartWetFloorEffect(args.Sprite, component.WetFloorEffectAlpha);
            }
            else
            {
                // If we have the effect but don't need it - end it
                if (args.Sprite.LayerGetState(0) == "sparkles")
                    EndWetFloorEffect(args.Sprite, component.OriginalRsi);
            }
        }

        private void StartWetFloorEffect(SpriteComponent sprite, float alpha)
        {
            sprite.LayerSetState(0, "sparkles", "Fluids/wet_floor_sparkles.rsi");
            sprite.Color = sprite.Color.WithAlpha(alpha);
            sprite.LayerSetAutoAnimated(0, false);
            sprite.LayerSetAutoAnimated(0, true); //fixes a bug where the sparkle effect would sometimes freeze on a single frame.
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
