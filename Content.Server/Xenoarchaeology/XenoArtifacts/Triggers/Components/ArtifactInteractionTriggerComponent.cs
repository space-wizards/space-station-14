using Robust.Shared.GameObjects;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;

/// <summary>
///     Activate artifact by touching, attacking or pulling it.
/// </summary>
[RegisterComponent]
public class ArtifactInteractionTriggerComponent : Component
{
    /// <summary>
    ///     Should artifact be activated just by touching with empty hand?
    /// </summary>
    [DataField("emptyHandActivation")]
    public bool EmptyHandActivation = true;

    /// <summary>
    ///     Should artifact be activated by melee attacking?
    /// </summary>
    [DataField("attackActivation")]
    public bool AttackActivation = true;

    /// <summary>
    ///     Should artifact be activated by starting pulling it?
    /// </summary>
    [DataField("pullActivation")]
    public bool PullActivation = true;
}
