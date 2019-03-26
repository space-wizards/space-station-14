using Content.Shared.GameObjects.Components.Mobs;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Maths;

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