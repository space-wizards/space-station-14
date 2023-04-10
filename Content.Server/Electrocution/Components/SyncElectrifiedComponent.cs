namespace Content.Server.Electrocution;

/// <summary>
/// Updates every frame to check if electrifed entity is powered, e.g to play animation
/// </summary>
[RegisterComponent]
public sealed class SyncElectrifiedComponent : Component
{
    [ViewVariables]
    public bool Powered = false;
}
