using Content.Shared.Power.Components;
using JetBrains.Annotations;

namespace Content.Shared.Power.EntitySystems;

/// <summary>
/// Generic system that handles entities with <see cref="PowerStateComponent"/>.
/// Used for simple machines that only need to switch between "idle" and "working" power states.
/// </summary>
public sealed class PowerStateSystem : EntitySystem
{
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiverSystem = default!;

    private EntityQuery<PowerStateComponent> _powerStateQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerStateComponent, ComponentStartup>(OnComponentStartup);

        _powerStateQuery = GetEntityQuery<PowerStateComponent>();
    }

    private void OnComponentStartup(Entity<PowerStateComponent> ent, ref ComponentStartup args)
    {
        SetWorkingState(ent.Owner, ent.Comp.IsWorking);
    }

    /// <summary>
    /// Sets the working state of the entity, adjusting its power draw accordingly.
    /// </summary>
    /// <param name="ent">The entity to set the working state for.</param>
    /// <param name="working">Whether the entity should be in the working state.</param>
    [PublicAPI]
    public void SetWorkingState(Entity<PowerStateComponent?> ent, bool working)
    {
        if (!_powerStateQuery.Resolve(ent, ref ent.Comp))
            return;

        _powerReceiverSystem.SetLoad(ent.Owner, working ? ent.Comp.WorkingPowerDraw : ent.Comp.IdlePowerDraw);
        ent.Comp.IsWorking = working;
    }
}
