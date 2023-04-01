using Content.Shared.Medical.Surgery;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;

namespace Content.Client.UserInterface.Systems.Surgery;

public sealed class SurgeryRealmOverlay : Overlay
{
    private readonly IEntityManager _entityManager;
    private readonly IEyeManager _eyeManager;
    private readonly Font _font;

    public SurgeryRealmOverlay(IEntityManager entityManager, IEyeManager eyeManager, IResourceCache resourceCache)
    {
        _entityManager = entityManager;
        _eyeManager = eyeManager;
        ZIndex = 200;
        _font = new VectorFont(resourceCache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);
    }

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!EntitySystem.TryGet(out EntityLookupSystem? entityLookup))
            return;

        var viewport = args.WorldAABB;

        foreach (var heart in _entityManager.EntityQuery<SurgeryRealmHeartComponent>())
        {
            // if not on the same map, continue
            if (_entityManager.GetComponent<TransformComponent>(heart.Owner).MapID != _eyeManager.CurrentMap)
            {
                continue;
            }

            var aabb = entityLookup.GetWorldAABB(heart.Owner);

            // if not on screen, continue
            if (!aabb.Intersects(in viewport))
            {
                continue;
            }

            var lineoffset = new Vector2(0f, 11f);
            var screenCoordinates = _eyeManager.WorldToScreen(aabb.Center +
                                                              new Angle(-_eyeManager.CurrentEye.Rotation).RotateVec(
                                                                  aabb.TopRight - aabb.Center)) + new Vector2(1f, 7f);
            args.ScreenHandle.DrawString(_font, screenCoordinates + lineoffset * 2, $"Health: {heart.Health}", Color.OrangeRed);
        }
    }
}
