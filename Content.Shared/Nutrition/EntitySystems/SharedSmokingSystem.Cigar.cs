using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;
using Content.Shared.Smoking;

namespace Content.Shared.Nutrition.EntitySystems;

public abstract partial class SharedSmokingSystem
{
    private void InitializeSharedCigars()
    {
        SubscribeLocalEvent<CigarComponent, AfterInteractEvent>(OnCigarAfterInteract);
    }

    private void OnCigarAfterInteract(Entity<CigarComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Target is null ||
            !args.CanReach ||
            !TryComp(entity, out SmokableComponent? smokable) ||
            smokable.State == SmokableState.Lit)
            return;

        if (TryDipCigar(entity, smokable, ref args))
        {
            args.Handled = true;
            return;
        }

        TryLightCigarFromInteraction(entity, smokable, ref args);
    }

    protected virtual void TryLightCigarFromInteraction(Entity<CigarComponent> entity, SmokableComponent smokable, ref AfterInteractEvent args)
    {
        // server smoking system provides existing cig lighting functionality
    }
}
