using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for a xenoarch trigger that activates after any type of physical interaction.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(XATInteractionSystem))]
public sealed partial class XATInteractionComponent : Component;
