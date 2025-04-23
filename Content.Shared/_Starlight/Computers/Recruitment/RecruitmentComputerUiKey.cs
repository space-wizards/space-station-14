using Content.Shared.Roles;
using Content.Shared.Starlight.Antags.Abductor;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Toolshed.TypeParsers;
using static Content.Shared.Pinpointer.SharedNavMapSystem;

namespace Content.Shared._Starlight.Computers.Recruitment;
[Serializable, NetSerializable]
public enum RecruitmentComputerUiKey : byte
{
    Key,
}
[Serializable, NetSerializable]
public sealed class RecruitmentChangeBuiMsg : BoundUserInterfaceMessage
{
    public required NetEntity Station { get; init; }
    public required ProtoId<JobPrototype> Job { get; init; }
    public required int Amount { get; init; }

}