namespace Content.Shared.Rotation;

public abstract class SharedRotationVisualsSystem : EntitySystem
{
    /// <summary>
    /// Sets the rotation an entity will have when it is "horizontal"
    /// <question>
    ///     why are we doing this? We're setting the horizontal angle to 90 degrees, when the object already has a Horizontal rotation 90 degrees
    ///         instead, should we not be changing the default rotation to be either horizontal or vertical?
    /// </question>
    /// </summary>
    public void SetHorizontalAngle(Entity<RotationVisualsComponent?> ent, Angle angle)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.DefaultRotation.Equals(angle))
            return;

        //ent.Comp.HorizontalRotation = angle;
        ent.Comp.DefaultRotation = ent.Comp.HorizontalRotation;

        //why do we do this here, but not in the method below?
        //Dirty(ent);
    }


    /// <summary>
    /// Resets the rotation an entity will have when it is "horizontal" back to it's default value.
    /// </summary>
    public void ResetHorizontalAngle(Entity<RotationVisualsComponent?> ent)
    {
        if (Resolve(ent, ref ent.Comp, false))
            //SetHorizontalAngle(ent, ent.Comp.DefaultRotation); //redo this shit, it's so lazy
            ent.Comp.DefaultRotation = ent.Comp.VerticalRotation;
    }
}
