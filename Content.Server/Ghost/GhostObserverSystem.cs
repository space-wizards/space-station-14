using System.Numerics;
using Content.Shared.Eye;
using Content.Shared.Ghost.Components;

namespace Content.Server.Ghost;

public sealed partial class GhostObserverSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostObserverComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, GhostObserverComponent component, ComponentStartup args)
    {
        if (TryComp(uid, out EyeComponent? eye))
        {
            _eye.SetVisibilityMask(uid, eye.VisibilityMask | (int) (VisibilityFlags.Ghost), eye);
        }
    }
}
