namespace Content.Server.MachineLinking.Components;

/// <summary>
/// Added to people holding a release signaller for detonating when dropped..
/// </summary>
[RegisterComponent]
public sealed class ReleaseSignallerHolderComponent : Component
{
    public HashSet<ReleaseSignallerComponent> Switches = new();
}
