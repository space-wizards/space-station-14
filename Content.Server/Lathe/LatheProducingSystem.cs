using Content.Server.Lathe.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Server.Lathe;

/// <summary>
/// System for handling lathes that are actively producing items.
/// The component is used more so as a marker for EntityQueryEnumerator,
/// however it's also used to set the power state of the lathe when producing.
/// </summary>
public sealed class LatheProducingSystem : EntitySystem
{
    [Dependency] private readonly SharedPowerStateSystem _powerState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LatheProducingComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<LatheProducingComponent, ComponentShutdown>(OnComponentShutdown);
    }

    private void OnComponentShutdown(Entity<LatheProducingComponent> ent, ref ComponentShutdown args)
    {
        // use the Try variant of this here
        // or else you get trolled by AllComponentsOneToOneDeleteTest
        _powerState.TrySetWorkingState(ent.Owner, false);
    }

    private void OnComponentStartup(Entity<LatheProducingComponent> ent, ref ComponentStartup args)
    {
        _powerState.TrySetWorkingState(ent.Owner, true);
    }
}
