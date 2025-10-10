using Content.Client.Decals;
using Content.Client.Decals.Overlays;
using Content.Shared.Decals;
using Content.Shared.Interaction;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Enums;

namespace Content.Client.Crayon.Overlays;

public sealed class CrayonDecalPlacementOverlay : DecalPlacementOverlay
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    private readonly SharedInteractionSystem _interaction;

    private readonly DecalPrototype? _decal;
    private readonly Angle _rotation;
    private readonly Color _color;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public CrayonDecalPlacementOverlay(DecalPlacementSystem placement, SharedTransformSystem transform, SpriteSystem sprite, SharedInteractionSystem interaction, DecalPrototype? decal, Angle rotation, Color color)
        : base(placement, transform, sprite)
    {
        IoCManager.InjectDependencies(this);
        _interaction = interaction;
        _decal = decal;
        _rotation = rotation;
        _color = color;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        var mouseScreenPos = _inputManager.MouseScreenPosition;
        var mousePos = _eyeManager.PixelToMap(mouseScreenPos);

        var playerEnt = _playerManager.LocalSession?.AttachedEntity;
        if (playerEnt == null)
            return false;

        // only show preview decal if it is within range to be drawn
        return _interaction.InRangeUnobstructed(mousePos, playerEnt.Value, collisionMask: Shared.Physics.CollisionGroup.None);
    }

    protected override void LoadDecal()
    {
        decal = _decal;
        snap = false;
        rotation = _rotation;
        color = _color;
    }
}
