namespace Content.Server.VoiceMask;

[RegisterComponent]
public sealed class VoiceMaskComponent : Component
{
    public bool Enabled = true;

    public string VoiceName = "Unknown";
}
