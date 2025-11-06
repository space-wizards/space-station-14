using Content.Shared.Popups;
using Content.Shared.Construction.Components;

namespace Content.Shared.Gravity;

public abstract class SharedGravityGeneratorSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GravityGeneratorComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
    }

    /// <summary>
    /// Prevent unanchoring when gravity is active
    /// </summary>
    private void OnUnanchorAttempt(Entity<GravityGeneratorComponent> ent, ref UnanchorAttemptEvent args)
    {
        if (!ent.Comp.GravityActive)
            return;

        _popupSystem.PopupClient(Loc.GetString("gravity-generator-unanchoring-failed"), ent.Owner, args.User, PopupType.Medium);

        args.Cancel();
    }
}
