using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Dice;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedDiceSystem))]
public sealed class DiceComponent : Component
{
    [DataField("sound")]
    public SoundSpecifier Sound { get; } = new SoundCollectionSpecifier("Dice");

    [DataField("step")]
    public int Step { get; } = 1;

    [DataField("sides")]
    public int Sides { get; } = 20;

    [DataField("currentSide")]
    public int CurrentSide { get; set; } = 20;

    [Serializable, NetSerializable]
    public sealed class DiceState : ComponentState
    {
        public int CurrentSide { get; set; } = 20;
        public DiceState(int side)
        {
            CurrentSide = side;
        }
    }
}
