using Content.Shared.Weapons.Misc;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Weapons.Misc;

public sealed class TetherGunSystem : SharedTetherGunSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TetheredComponent, ComponentStartup>(OnTetheredStartup);
        SubscribeLocalEvent<TetheredComponent, ComponentShutdown>(OnTetheredShutdown);
        _overlay.AddOverlay(new TetherGunOverlay(EntityManager));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<TetherGunOverlay>();
    }

    private void OnTetheredStartup(EntityUid uid, TetheredComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.Color = Color.Orange;
    }

    private void OnTetheredShutdown(EntityUid uid, TetheredComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.Color = Color.White;
    }
}
