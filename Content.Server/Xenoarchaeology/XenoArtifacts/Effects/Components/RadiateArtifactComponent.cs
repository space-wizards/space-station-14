using Content.Server.Radiation;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
///     Spawn RadiationPulse when artifact activated.
/// </summary>
[RegisterComponent]
public class RadiateArtifactComponent : Component
{
    public override string Name => "RadiateArtifact";

    /// <summary>
    ///     Radiation pulse prototype to spawn.
    ///     Should has <see cref="RadiationPulseComponent"/>.
    /// </summary>
    [DataField("pulsePrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string PulsePrototype = "RadiationPulse";
}
