using Content.Shared.Emag.Systems;

/// <summary>
/// System for vapes
/// </summary>
namespace Content.Shared.Smoking;

public abstract partial class SharedSmokingSystem
{
    private void InitializeVapes()
    {
        SubscribeLocalEvent<VapeComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnEmagged(Entity<VapeComponent> ent, ref GotEmaggedEvent args)
    {
        args.Handled = true;
    }
}
