using System;
using System.Linq;
using Content.Shared.Fluids;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;


namespace Content.Client.Fluids
{
    [UsedImplicitly]
    public class PuddleVisualizer : AppearanceVisualizer
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        // Whether the underlying solution color should be used
        [DataField("recolor")] public bool Recolor;

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            IoCManager.InjectDependencies(this);

            var spriteComponent = entity.EnsureComponent<SpriteComponent>();
            var maxStates = spriteComponent.BaseRSI?.ToArray() ?? Array.Empty<RSI.State>();

            var variant = maxStates.Length switch
            {
                > 1 => _random.Next(0, maxStates.Length - 1),
                1 => 0,
                _ => 0,
            };
            spriteComponent.LayerSetState(0, maxStates[variant].StateId);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.TryGetData<float>(PuddleVisual.VolumeScale, out var volumeScale) &&
                component.Owner.TryGetComponent<SpriteComponent>(out var spriteComponent))
            {
                var cappedScale = Math.Min(1.0f, volumeScale * 0.75f +0.25f);
                UpdateVisual(component, spriteComponent, cappedScale);
            }
        }

        private void UpdateVisual(AppearanceComponent component, SpriteComponent spriteComponent, float cappedScale)
        {
            Color newColor;
            if (Recolor && component.TryGetData<Color>(PuddleVisual.SolutionColor, out var solutionColor))
            {
                newColor = solutionColor.WithAlpha(cappedScale);
            }
            else
            {
                newColor = spriteComponent.Color.WithAlpha(cappedScale);
            }

            spriteComponent.Color = newColor;
        }
    }
}
