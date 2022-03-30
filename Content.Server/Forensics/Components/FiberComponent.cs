namespace Content.Server.Forensics
{
  [RegisterComponent]
  public sealed class FiberComponent : Component
  {
    [DataField("fiberDescription")]
    public string FiberDescription = "synthetic fibers";
  }
}
