using System;
using System.Linq;
using Content.Shared.Fluids;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
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

        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out SpriteComponent? spriteComponent))
            {
                Logger.Warning($"Missing SpriteComponent for PuddleVisualizer on entityUid = {entity}");
                return;
            }

            IoCManager.InjectDependencies(this);

            var maxStates = spriteComponent.BaseRSI?.ToArray();

            if (maxStates is not { Length: > 0 }) return;

            var variant = _random.Next(0, maxStates.Length - 1);
            spriteComponent.LayerSetState(0, maxStates[variant].StateId);
            spriteComponent.Rotation = Angle.FromDegrees(_random.Next(0, 359));
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (component.TryGetData<float>(PuddleVisuals.VolumeScale, out var volumeScale) &&
                entities.TryGetComponent<SpriteComponent>(component.Owner, out var spriteComponent))
            {
                var cappedScale = Math.Min(1.0f, volumeScale * 0.75f +0.25f);
                UpdateVisual(component, spriteComponent, cappedScale);
            }
        }

        private void UpdateVisual(AppearanceComponent component, SpriteComponent spriteComponent, float cappedScale)
        {
            Color newColor;
            if (Recolor && component.TryGetData<Color>(PuddleVisuals.SolutionColor, out var solutionColor))
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
