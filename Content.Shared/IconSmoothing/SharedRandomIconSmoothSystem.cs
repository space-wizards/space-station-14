namespace Content.Shared.IconSmoothing;

public abstract class SharedRandomIconSmoothSystem : EntitySystem
{
    protected virtual void UpdateVisualState(Entity<RandomIconSmoothComponent> ent, string newState) { }
}
