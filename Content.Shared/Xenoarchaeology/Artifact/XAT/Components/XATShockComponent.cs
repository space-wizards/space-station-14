using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for a xenoarch trigger that activates when receiving an electrical shock.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(XATShockSystem))]
public sealed partial class XATShockComponent : Component
{
}
