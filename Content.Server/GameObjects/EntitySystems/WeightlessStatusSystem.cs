using Robust.Shared.GameObjects.Systems;

public class WeightlessStatusSystem : EntitySystem
{

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var component in ComponentManager.EntityQuery<WeightlessStatusComponent>())
        {
            component.Update();
        }
    }
}
