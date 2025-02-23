namespace Content.Shared.Containers;

public sealed class DunkableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DunkableComponent, ThrownIntoContainerEvent>(OnBeforeThrownIntoContainerThrowerEvent);
    }

    private void OnBeforeThrownIntoContainerThrowerEvent(Entity<DunkableComponent> ent, ref ThrownIntoContainerEvent args)
    {
        args.Modifier *= ent.Comp.Dunkability;
    }
}
