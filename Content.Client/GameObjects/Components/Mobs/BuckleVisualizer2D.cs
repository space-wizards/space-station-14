using Content.Shared.GameObjects.Components.Mobs;
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
            if (component.TryGetData<SharedSpeciesComponent.MobState>(SharedSpeciesComponent.MobVisuals.RotationState, out var state))
            {
                switch (state)
                {
                    case SharedSpeciesComponent.MobState.Down:
                        SetRotation(component, Angle.FromDegrees(-90));
                        break;
                }
            }
        }
    }
}
