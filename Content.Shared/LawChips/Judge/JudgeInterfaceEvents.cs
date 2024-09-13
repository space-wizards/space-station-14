using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.LawChips.Judge;

[Serializable, NetSerializable]
public enum JudgeInterfaceVisuals : byte
{
    DeviceState,
    Screen,
    Broken
}
