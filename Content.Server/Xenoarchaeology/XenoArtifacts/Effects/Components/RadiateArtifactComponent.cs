using Content.Server.Radiation;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
///     Spawn RadiationPulse when artifact activated.
/// </summary>
[RegisterComponent]
public sealed class RadiateArtifactComponent : Component
{
    public override string Name => "RadiateArtifact";

    /// <summary>
    ///     Radiation pulse prototype to spawn.
    ///     Should has <see cref="RadiationPulseComponent"/>.
    /// </summary>
    [DataField("pulsePrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string PulsePrototype = "RadiationPulse";
}
