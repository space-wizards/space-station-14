using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.UserInterface;
using System.Linq;
using Content.Shared.DoAfter;
using Content.Shared.Starlight.Medical.Surgery;
using Robust.Shared.Spawners;
using Content.Shared.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Mind.Components;
using Content.Shared.Mind;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared._Starlight.Computers.Recruitment;
using Content.Server.Station.Components;
using Content.Shared.Pinpointer;
using Content.Server.Station.Systems;

namespace Content.Server._Starlight.Computers.Recruitment;

public sealed partial class RecruitmentSystem : EntitySystem
{
    [Dependency] private readonly StationJobsSystem _jobsSystem = default!;

    public override void Initialize() 
        => Subs.BuiEvents<RecruitmentComputerComponent>(RecruitmentComputerUiKey.Key, subs => subs.Event<RecruitmentChangeBuiMsg>(OnChangeBuiMsg));

    private void OnChangeBuiMsg(EntityUid uid, RecruitmentComputerComponent component, RecruitmentChangeBuiMsg args) 
        => _jobsSystem.TryAdjustJobSlot(GetEntity(args.Station), args.Job, args.Amount, true, true);
}
