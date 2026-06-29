using Content.Shared.Administration.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Administration.Systems;

public sealed class HeadstandSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<HeadstandComponent, ComponentStartup>(OnHeadstandAdded);
        SubscribeLocalEvent<HeadstandComponent, ComponentShutdown>(OnHeadstandRemoved);
    }

    private void OnHeadstandAdded(EntityUid uid, HeadstandComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        foreach (var layer in sprite.AllLayers)
        {
            layer.Rotation += Angle.FromDegrees(180.0f);
        }
    }

    private void OnHeadstandRemoved(EntityUid uid, HeadstandComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        foreach (var layer in sprite.AllLayers)
        {
            layer.Rotation -= Angle.FromDegrees(180.0f);
        }
    }
}
