using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.LawChips.Chips;

[RegisterComponent, NetworkedComponent]
public sealed partial class LawChipComponent : Component
{
    [DataField("isPrinted")]
    public bool IsPrinted = false;
}

[Serializable, NetSerializable]
public enum LawChipAppearance : byte
{
    Blank,
    Normal,
    Defective,
    Hacked,
    Custom
}
