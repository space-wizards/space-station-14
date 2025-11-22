using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for an artifact trigger that activates when a thrown item lands on the ground.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(XATItemLandSystem))]
public sealed partial class XATItemLandComponent : Component;
