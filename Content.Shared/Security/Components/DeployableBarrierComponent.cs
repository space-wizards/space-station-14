using Content.Shared.Security.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Security.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(DeployableBarrierSystem))]
public sealed partial class DeployableBarrierComponent : Component
{
    /// <summary>
    ///     The fixture to change collision on.
    /// </summary>
    [DataField("fixture", required: true)] public string FixtureId = string.Empty;
}
