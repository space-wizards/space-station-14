using Robust.Shared.GameStates;

namespace Content.Server.Fake;

/// <summary>
/// psychosis thing
/// </summary>
[RegisterComponent]
[NetworkedComponent]
[Access(typeof(FakeSystem))]
public sealed partial class FakeComponent : Component
{
    public TimeSpan Difference = TimeSpan.FromSeconds(1);
    public TimeSpan Next = TimeSpan.FromSeconds(1);
}
