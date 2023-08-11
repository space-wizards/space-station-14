namespace Content.Client.Power.Generation.Teg;

/// <summary>
/// A simple component that causes its entity to disappear after a few seconds.
/// </summary>
/// <seealso cref="TegSystem"/>
[RegisterComponent]
public sealed class TegCirculatorArrowComponent : Component
{
    public TimeSpan DestroyTime;
}
