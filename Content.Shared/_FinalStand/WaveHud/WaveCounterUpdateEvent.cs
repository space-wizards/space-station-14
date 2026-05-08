using Robust.Shared.Serialization;

namespace Content.Shared._FinalStand.WaveHud;

[Serializable, NetSerializable]
public sealed class WaveCounterUpdateEvent : EntityEventArgs
{
    public readonly int Wave;
    public WaveCounterUpdateEvent(int wave) => Wave = wave;
}
