using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Implants.Components;

namespace Content.Shared.Implants;

/// <summary>
/// SharedImplantLoaderSystem covers the core behaviour of handling the loading of implanters with new charges.
/// </summary>
public abstract class SharedImplantLoaderSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ImplantLoaderComponent, SolutionContainerChangedEvent>(OnSolutionChange);
    }

    protected virtual void OnSolutionChange(EntityUid uid, ImplantLoaderComponent component, SolutionContainerChangedEvent args)
    {
        Dirty(uid, component);
    }
}
