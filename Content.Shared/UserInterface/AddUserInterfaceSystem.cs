namespace Content.Shared.UserInterface;

/// <remarks>
/// Engine issue #5141 being implemented would make this obsolete
/// </remarks>
public sealed class AddUserInterfaceSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddUserInterfaceComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<AddUserInterfaceComponent> ent, ref MapInitEvent args)
    {
        foreach (var (key, iface) in ent.Comp.Interfaces)
        {
            _ui.SetUi(ent.Owner, key, iface);
        }

        RemComp<AddUserInterfaceComponent>(ent);
    }
}
