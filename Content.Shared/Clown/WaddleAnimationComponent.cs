using System.Numerics;

namespace Content.Shared.Clown;

public struct ClownStartedWalkingEvent(EntityUid entity)
{
    public EntityUid Entity = entity;
}

public struct ClownStoppedWalkingEvent(EntityUid entity)
{
    public EntityUid Entity = entity;
}

/// <summary>
/// Defines something as having a waddle animation when it moves.
/// </summary>
[RegisterComponent]
public sealed partial class WaddleAnimationComponent : Component
{
    [DataField]
    public string KeyName = "ClownWalk";

    [DataField]
    public Vector2 HopIntensity = new(0, 0.25f);

    [DataField]
    public float TumbleIntensity = 15.0f;

    [DataField]
    public float AnimationLength = 0.666f;

    // Just allows us to alternate between left and right steps
    public bool LastStep;
}
