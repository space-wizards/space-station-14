using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedKitchenSpikeSystem))]
public sealed partial class KitchenSpikeComponent : Component
{
    [DataField("delay")]
    public float SpikeDelay = 7.0f;

    [DataField("sound")]
    public SoundSpecifier SpikeSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

    [DataField]
    public List<string>? PrototypesToSpawn;

    // TODO: Spiking alive mobs? (Replace with uid) (deal damage to their limbs on spiking, kill on first butcher attempt?)
    [DataField]
    public string MeatSource1p = "?";

    [DataField]
    public string MeatSource0 = "?";

    [DataField]
    public string? VictimName = null;

    [DataField]
    public string? VictimDna = null;

    // Prevents simultaneous spiking of two bodies (could be replaced with CancellationToken, but I don't see any situation where Cancel could be called)
    [DataField]
    public bool InUse;

    [Serializable, NetSerializable]
    public enum KitchenSpikeVisuals : byte
    {
        Status
    }

    [Serializable, NetSerializable]
    public enum KitchenSpikeStatus : byte
    {
        Empty,
        Bloody
    }
}
