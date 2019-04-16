using Content.Shared.GameObjects.Components.Mobs;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Mobs
{
    public class SpeciesVisualizer2D : AppearanceVisualizer
    {
        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (component.TryGetData<SharedSpeciesComponent.MobState>(SharedSpeciesComponent.MobVisuals.RotationState, out var state))
            {
                switch (state) 
                {
                    case SharedSpeciesComponent.MobState.Stand:
                        sprite.Rotation = 0;
                        break;
                    case SharedSpeciesComponent.MobState.Down:
                        sprite.Rotation = Angle.FromDegrees(90);
                        break;
                }
            }
        }
    }
}