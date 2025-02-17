using Content.Shared.Actions;
using Content.Shared.DeadSpace.Abilities.WallHack.Components;

namespace Content.Server.DeadSpace.Abilities.WallHack;

public sealed partial class WallHackSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WallHackComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<WallHackComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnComponentInit(EntityUid uid, WallHackComponent component, ComponentInit args)
    {
        if (EntityManager.TryGetComponent<EyeComponent>(uid, out var eyeComp))
        {
            _eye.SetDrawLight((uid, eyeComp), false);
        }
    }

    private void OnShutdown(EntityUid uid, WallHackComponent component, ComponentShutdown args)
    {
        if (EntityManager.TryGetComponent<EyeComponent>(uid, out var eyeComp))
        {
            _eye.SetDrawLight((uid, eyeComp), true);
        }
    }
}
