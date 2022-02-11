using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Xenoarchaeology.XenoArtifacts;

[RegisterComponent]
public class ArtifactComponent : Component
{
    /// <summary>
    ///     Should artifact pick a random trigger on startup?
    /// </summary>
    [DataField("randomTrigger")]
    public bool RandomTrigger = true;

    /// <summary>
    ///     List of all possible triggers activations.
    ///     Should be same as components names.
    /// </summary>
    [DataField("possibleTriggers")]
    public string[] PossibleTriggers = {
        "ArtifactInteractionTrigger",
        "ArtifactGasTrigger"
    };

    /// <summary>
    ///     Cooldown time between artifact activations (in seconds).
    /// </summary>
    [DataField("timer")]
    [ViewVariables(VVAccess.ReadWrite)]
    public double CooldownTime = 10;

    public TimeSpan LastActivationTime;
}
