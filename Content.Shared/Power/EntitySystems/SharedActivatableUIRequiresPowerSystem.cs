using Content.Shared.Power.Components;
using Content.Shared.UserInterface;

namespace Content.Shared.Power.EntitySystems;

public abstract class SharedActivatableUIRequiresPowerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActivatableUIRequiresPowerComponent, ActivatableUIOpenAttemptEvent>(OnActivate);
        SubscribeLocalEvent<ActivatableUIRequiresPowerComponent, VerbUIOpenAttemptEvent>(OnActivateVerb);
    }

    protected abstract void OnActivate(Entity<ActivatableUIRequiresPowerComponent> ent, ref ActivatableUIOpenAttemptEvent args);

    protected abstract void OnActivateVerb(Entity<ActivatableUIRequiresPowerComponent> ent, ref VerbUIOpenAttemptEvent args);
}
