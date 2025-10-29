using Content.Shared.DeadSpace.Abilities.Bloodsucker.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DeadSpace.Abilities.Bloodsucker;
using Robust.Shared.GameStates;
using Content.Server.Body.Systems;

namespace Content.Server.DeadSpace.Abilities.Bloodsucker;

public sealed partial class BloodIncubatorSystem : EntitySystem
{
    [Dependency] private readonly BloodsuckerSystem _bloodsucker = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodIncubatorComponent, BloodsuckEvent>(OnBloodsuck);
        SubscribeLocalEvent<BloodIncubatorComponent, ComponentStartup>(
            OnStart,
            before: new[] { typeof(BloodsuckerSystem), typeof(BloodstreamSystem), typeof(SharedSolutionContainerSystem) }
        );
        SubscribeLocalEvent<BloodIncubatorComponent, ModifyBloodLevelEvent>(OnModifyBloodLevel);
        SubscribeLocalEvent<BloodIncubatorComponent, ComponentGetState>(OnBloodIncubatorGetState);
    }

    private void OnStart(EntityUid uid, BloodIncubatorComponent component, ComponentStartup args)
    {
        UpdateBloodIncubator(uid, component);
    }

    private void OnBloodIncubatorGetState(EntityUid uid, BloodIncubatorComponent component, ref ComponentGetState args)
    {
        if (!TryComp<BloodsuckerComponent>(uid, out var bloodsucker))
            return;

        if (component.States.Count == 0)
            return;

        var progress = Math.Clamp(bloodsucker.CountReagent / bloodsucker.MaxCountReagent, 0f, 1f);

        var index = (int)MathF.Round(progress * (component.States.Count - 1));

        args.State = new BloodIncubatorComponentState(index);
    }

    private void OnBloodsuck(EntityUid uid, BloodIncubatorComponent component, BloodsuckEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(uid, out BloodstreamComponent? bloodstreamComponent))
            return;

        if (!_bloodsucker.TryModifyBloodLevel(uid, args.Quantity, bloodstreamComponent))
            return;

        if (!TryComp<BloodsuckerComponent>(uid, out var bloodsucker))
            return;

        UpdateBloodIncubator(uid, component);
    }

    private void OnModifyBloodLevel(EntityUid uid, BloodIncubatorComponent component, ModifyBloodLevelEvent args)
    {
        if (args.Handled)
            return;

        UpdateBloodIncubator(uid, component);
    }

    private void UpdateBloodIncubator(EntityUid uid, BloodIncubatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!TryComp(uid, out BloodstreamComponent? bloodstreamComponent))
            return;

        if (!TryComp<BloodsuckerComponent>(uid, out var bloodsucker))
            return;

        if (!_solutionContainer.ResolveSolution(uid, bloodstreamComponent.BloodSolutionName, ref bloodstreamComponent.BloodSolution))
            return;

        _bloodsucker.SetReagentCount(uid, (float)bloodstreamComponent.BloodSolution.Value.Comp.Solution.Volume);

        Dirty(uid, component);
    }

}
