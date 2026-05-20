using System.Linq;
using System.Numerics;
using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Decals.Overlays;

public sealed class DecalCopyOverlay : Overlay
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private readonly SharedTransformSystem _transform;
    private readonly SpriteSystem _sprite;
    private readonly SharedDecalSystem _sharedDecalSystem;

    public Decal CurrentDecal { get; private set; } = new();
    internal short DecalIndex = new();

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    public DecalCopyOverlay()
    {
        IoCManager.InjectDependencies(this);

        _transform = _entityManager.System<SharedTransformSystem>();
        _sprite = _entityManager.System<SpriteSystem>();
        _sharedDecalSystem = _entityManager.System<SharedDecalSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var mouseScreenPos = _inputManager.MouseScreenPosition;
        var mousePos = _eyeManager.PixelToMap(mouseScreenPos);

        if (mousePos.MapId != args.MapId)
            return;

        if (!_mapManager.TryFindGridAt(mousePos, out var gridUid, out var grid))
            return;

        var worldMatrix = _transform.GetWorldMatrix(gridUid);
        var invMatrix = _transform.GetInvWorldMatrix(gridUid);

        var handle = args.WorldHandle;
        handle.SetTransform(worldMatrix);

        var localPos = Vector2.Transform(mousePos.Position, invMatrix);

        var decals = _sharedDecalSystem.GetDecalsInRange(gridUid, localPos, 0.5f);

        if (decals.Count == 0)
            return;

        var decalList = decals.Select(d => d.Decal).ToList();

        // < 0 Just for be sure, what smbd willnt set it to -1
        if (DecalIndex < 0 || DecalIndex > decalList.Count - 1)
            DecalIndex = (short)(decalList.Count - 1);

        CurrentDecal = decalList[DecalIndex];

        var prototype = _prototypeManager.Index<DecalPrototype>(CurrentDecal.Id);

        var aabb = Box2.UnitCentered.Translated(localPos).Scale(1.2f);
        var box = new Box2Rotated(aabb, CurrentDecal.Angle, localPos);

        handle.DrawTextureRect(_sprite.Frame0(prototype.Sprite), box, CurrentDecal.Color);
        handle.SetTransform(Matrix3x2.Identity);
    }
}
