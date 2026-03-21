using System.Numerics;
using Content.Shared.Carrying.Components;
using Content.Shared.Carrying.Systems;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Carrying;

internal sealed class CarryingSystem : SharedCarryingSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeingCarriedComponent, ComponentStartup>(OnBeingCarriedStartup);
        SubscribeLocalEvent<BeingCarriedComponent, ComponentRemove>(OnBeingCarriedRemove);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<BeingCarriedComponent, CarriableComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var carriable, out var xform))
        {
            if (carriable.CarriedBy is not { } carrier)
                continue;

            if (!TryComp<CarrierComponent>(carrier, out var carrierComp))
                continue;

            // Local coordinates rotate with the parent, so counter-rotate the offset
            // to keep the carried entity visually above the carrier in screen space.
            var carrierXform = Transform(carrier);
            var worldOffset = new Vector2(0, carrierComp.CarryOffset);
            var localOffset = (-carrierXform.LocalRotation).RotateVec(worldOffset);

            _xform.SetLocalPosition(uid, localOffset, xform);
        }
    }

    private void OnBeingCarriedStartup(EntityUid uid, BeingCarriedComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        component.OriginalDrawDepth ??= sprite.DrawDepth;
        _sprite.SetDrawDepth((uid, sprite), (int) DrawDepth.OverMobs);
    }

    private void OnBeingCarriedRemove(EntityUid uid, BeingCarriedComponent component, ComponentRemove args)
    {
        if (!component.OriginalDrawDepth.HasValue)
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _sprite.SetDrawDepth((uid, sprite), component.OriginalDrawDepth.Value);
        component.OriginalDrawDepth = null;
    }
}
