namespace Content.Shared.NPC;

public abstract class SharedNPCSteeringSystem : EntitySystem
{
    public const byte InterestDirections = 12;

    /// <summary>
    /// How many radians between each interest direction.
    /// </summary>
    public const double InterestRadians = MathF.Tau / InterestDirections;

    /// <summary>
    /// How many degrees between each interest direction.
    /// </summary>
    public const float InterestDegrees = 360f / InterestDirections;
}
}
