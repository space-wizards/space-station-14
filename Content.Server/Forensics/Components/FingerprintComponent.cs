namespace Content.Server.Forensics
{
  [RegisterComponent]
  public sealed class FingerprintComponent : Component
  {
    [DataField("fingerprint")]
    public string? Fingerprint;
  }
}
