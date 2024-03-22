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

[RegisterComponent]
public sealed partial class WaddleAnimationComponent : Component
{
    [DataField]
    public string KeyName = "ClownWalk";

    [DataField]
    public Vector2 HopIntensity = new(0, 0.5f);

    [DataField]
    public float TumbleIntensity = 30.0f;

    // Just allows us to alternate between left and right steps
    public bool LastStep;
}
