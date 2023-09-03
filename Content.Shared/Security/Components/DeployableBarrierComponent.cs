namespace Content.Shared.Security.Components;

[RegisterComponent]
public sealed partial class DeployableBarrierComponent : Component
{
    /// <summary>
    ///     The fixture to change collision on.
    /// </summary>
    [DataField("fixture", required: true)] public string FixtureId = string.Empty;
}

