using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.LawChips.Judge;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedJudgeInterfaceSystem))]
[AutoGenerateComponentState]
public sealed partial class JudgeInterfaceComponent : Component
{
    [DataField("status"), AutoNetworkedField]
    public JudgeInterfaceStatus Status = JudgeInterfaceStatus.Normal;

    public const string JudgeInterfaceLawChipSlotId = "JudgeInterface-LawChip";

    [DataField("chipSlot")]
    public ItemSlot ChipSlot = new();
}

[NetSerializable, Serializable]
public enum JudgeInterfaceStatus : byte
{
    Normal,
    Hacked,
    Broken
}
