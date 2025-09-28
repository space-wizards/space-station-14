using Content.Client.Decals;
using Content.Client.Decals.Overlays;
using Content.Shared.Decals;
using Content.Shared.Interaction;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;

namespace Content.Client._Starlight.Crayon.Overlays;

public sealed class CrayonDecalGhostOverlay : DecalPlacementOverlay
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    private readonly SharedInteractionSystem _interaction;
    private readonly DecalPrototype? _decalPrototype;
    private readonly Angle _rotation;
    private readonly Color _color;

    public CrayonDecalGhostOverlay(DecalPlacementSystem placement, SharedTransformSystem transform, SpriteSystem sprite, SharedInteractionSystem interaction, DecalPrototype? decalPrototype, Angle rotation, Color color) : base(placement, transform, sprite)
    {
        IoCManager.InjectDependencies(this);
        _interaction = interaction;
        _decalPrototype = decalPrototype;
        _rotation = rotation;
        _color = color;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        var mouseScreenPos = _inputManager.MouseScreenPosition;
        var mousePos = _eyeManager.PixelToMap(mouseScreenPos);

        var player = _playerManager.LocalEntity;
        if (player is null)
            return false;
        return _interaction.InRangeUnobstructed(mousePos,
            player.Value,
            collisionMask: Shared.Physics.CollisionGroup.None);
    }

    protected override void LoadDecal()
    {
        decal = _decalPrototype;
        snap = false;
        rotation = _rotation;
        color = _color;
    }
}
