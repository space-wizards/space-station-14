namespace Content.Shared.Rotation;

public abstract class SharedRotationVisualsSystem : EntitySystem
{
    /// <summary>
    /// Sets the rotation an entity will have when it is "horizontal"
    /// </summary>
    public void SetHorizontalAngle(Entity<RotationVisualsComponent?> ent, Angle angle)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.HorizontalRotation.Equals(angle))
            return;

        ent.Comp.HorizontalRotation = angle;
        Dirty(ent);
    }


    /// <summary>
    /// Resets the rotation an entity will have when it is "horizontal" back to it's default value.
    /// </summary>
    public void ResetHorizontalAngle(Entity<RotationVisualsComponent?> ent)
    {
        if (Resolve(ent, ref ent.Comp, false))
            SetHorizontalAngle(ent, ent.Comp.DefaultRotation);
    }
}
