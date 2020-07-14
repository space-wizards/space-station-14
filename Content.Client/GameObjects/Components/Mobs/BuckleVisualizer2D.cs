using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Strap;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Mobs
{
    [UsedImplicitly]
    public class BuckleVisualizer2D : SpeciesVisualizer2D
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            if (!component.TryGetData<bool>(SharedBuckleComponent.BuckleVisuals.Buckled, out var buckled) ||
                !buckled)
            {
                return;
            }

            if (!component.TryGetData<int>(StrapVisuals.RotationAngle, out var angle))
            {
                return;
            }

            SetRotation(component, Angle.FromDegrees(angle));
        }
    }
}
