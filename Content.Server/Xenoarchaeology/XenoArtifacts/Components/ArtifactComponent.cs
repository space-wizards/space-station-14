using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Components;

[RegisterComponent]
public class ArtifactComponent : Component
{
    public override string Name => "Artifact";

    /// <summary>
    ///     Cooldown time between artifact activations (in seconds).
    /// </summary>
    [DataField("timer")]
    [ViewVariables(VVAccess.ReadWrite)]
    public double CooldownTime = 10;

    public TimeSpan LastActivationTime;
}
