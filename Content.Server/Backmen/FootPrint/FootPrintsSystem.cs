// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT
// Official port from the BACKMEN project. Make sure to review the original repository to avoid license violations.

using System.Threading;
using System.Threading.Tasks;
using Content.Server.Atmos.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Backmen.FootPrint;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Gravity;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Backmen.FootPrint;

public sealed class FootPrintsSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly IMapManager _map = default!;

    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;

    private EntityQuery<TransformComponent> _transformQuery;
    private EntityQuery<MobThresholdsComponent> _mobThresholdQuery;
    private EntityQuery<AppearanceComponent> _appearanceQuery;

    public override void Initialize()
    {
        base.Initialize();

       _transformQuery = GetEntityQuery<TransformComponent>();
       _mobThresholdQuery = GetEntityQuery<MobThresholdsComponent>();
       _appearanceQuery = GetEntityQuery<AppearanceComponent>();

        SubscribeLocalEvent<FootPrintsComponent, ComponentStartup>(OnStartupComponent);
        SubscribeLocalEvent<FootPrintsComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<FootPrintComponent, ComponentGetState>(OnGetState);
    }


    private const double ActionJobTime = 0.005;
    private readonly JobQueue _actionJobQueue = new(ActionJobTime);

    private void OnGetState(Entity<FootPrintComponent> ent, ref ComponentGetState args)
    {
        args.State = new FootPrintState(TerminatingOrDeleted(ent.Comp.PrintOwner)
            ? NetEntity.Invalid
            : GetNetEntity(ent.Comp.PrintOwner));
    }

    private void OnStartupComponent(EntityUid uid, FootPrintsComponent comp, ComponentStartup args)
    {
        comp.StepSize += _random.NextFloat(-0.05f, 0.05f);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _actionJobQueue.Process();
    }

    public sealed class FootprintsMoveProcess(
        Entity<FootPrintsComponent> ent,
        FootPrintsSystem system,
        double maxTime,
        CancellationToken cancellation = default)
        : Job<object>(maxTime, cancellation)
    {
        protected override async Task<object?> Process()
        {
            system.MoveProccessing(ent);
            return null;
        }
    }

    private void OnMove(EntityUid uid, FootPrintsComponent comp, ref MoveEvent args)
    {
        // Less resource expensive checks first
        if (comp.PrintsColor.A <= 0f || TerminatingOrDeleted(uid))
            return;

        _actionJobQueue.EnqueueJob(new FootprintsMoveProcess((uid, comp), this, ActionJobTime));
    }

    private void MoveProccessing(Entity<FootPrintsComponent> ent)
    {
        var (uid, comp) = ent;
        var transform = Transform(uid);

        if (_gravity.IsWeightless(uid))
            return;

        if (!_mobThresholdQuery.TryComp(uid, out var mobThreshHolds) ||
            !_map.TryFindGridAt(_transform.GetMapCoordinates((uid, transform)), out var gridUid, out _))
            return;

        var dragging = mobThreshHolds.CurrentThresholdState is MobState.Critical or MobState.Dead;
        var distance = (transform.LocalPosition - comp.StepPos).Length();
        var stepSize = dragging ? comp.DragSize : comp.StepSize;

        if (!(distance > stepSize))
            return;

        comp.RightStep = !comp.RightStep;

        var entity = Spawn(comp.StepProtoId, CalcCoords(gridUid, comp, transform, dragging));
        var footPrintComponent =
            Comp<FootPrintComponent>(entity); // There's NO way there's no footprint component in a FOOTPRINT

        footPrintComponent.PrintOwner = uid;
        Dirty(entity, footPrintComponent);

        if (_appearanceQuery.TryComp(entity, out var appearance))
        {
            _appearance.SetData(entity, FootPrintVisualState.State, PickState(uid, dragging), appearance);
            _appearance.SetData(entity, FootPrintVisualState.Color, comp.PrintsColor, appearance);
        }

        if (!_transformQuery.TryComp(entity, out var stepTransform))
            return;

        stepTransform.LocalRotation = dragging
            ? (transform.LocalPosition - comp.StepPos).ToAngle() + Angle.FromDegrees(-90f)
            : transform.LocalRotation + Angle.FromDegrees(180f);

        comp.PrintsColor = comp.PrintsColor.WithAlpha(ReduceAlpha(comp.PrintsColor.A, comp.ColorReduceAlpha));
        comp.StepPos = transform.LocalPosition;

        if (!TryComp<SolutionContainerManagerComponent>(entity, out var solutionContainer)
            || !_solutionSystem.ResolveSolution((entity, solutionContainer),
                footPrintComponent.SolutionName,
                ref footPrintComponent.Solution,
                out var solution)
            || string.IsNullOrWhiteSpace(comp.ReagentToTransfer) || solution.Volume >= 1)
            return;

        _solutionSystem.TryAddReagent(footPrintComponent.Solution.Value, comp.ReagentToTransfer, 1, out _);
    }

    private EntityCoordinates CalcCoords(EntityUid uid,
        FootPrintsComponent comp,
        TransformComponent transform,
        bool state)
    {
        if (state)
            return new EntityCoordinates(uid, transform.LocalPosition);

        var offset = comp.RightStep
            ? new Angle(Angle.FromDegrees(180f) + transform.LocalRotation).RotateVec(comp.OffsetPrint)
            : new Angle(transform.LocalRotation).RotateVec(comp.OffsetPrint);

        return new EntityCoordinates(uid, transform.LocalPosition + offset);
    }

    private FootPrintVisuals PickState(EntityUid uid, bool dragging)
    {
        var state = FootPrintVisuals.BareFootPrint;

        if (_inventorySystem.TryGetSlotEntity(uid, "shoes", out _))
        {
            state = FootPrintVisuals.ShoesPrint;
        }

        if (_inventorySystem.TryGetSlotEntity(uid, "outerClothing", out var suit) &&
            TryComp<PressureProtectionComponent>(suit, out _))
        {
            state = FootPrintVisuals.SuitPrint;
        }

        if (dragging)
            state = FootPrintVisuals.Dragging;

        return state;
    }

    private float ReduceAlpha(float alpha, float reductionAmount)
    {
        if (alpha - reductionAmount > 0f)
        {
            alpha -= reductionAmount;
        }
        else
        {
            alpha = 0f;
        }

        return alpha;
    }
}
