using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.RCD;
using Content.Shared.RCD.Components;
using Content.Shared.RCD.Systems;
using Robust.Client.Placement;
using Robust.Client.Player;
using Robust.Shared.Enums;

namespace Content.Client.RCD;

public sealed class RCDConstructionGhostSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly RCDSystem _rcdSystem = default!;
    [Dependency] private readonly IPlacementManager _placementManager = default!;

    private string _placementMode = typeof(AlignRCDConstruction).Name;
    private Direction _placementDirection = default;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Get current placer data
        var placerEntity = _placementManager.CurrentPermission?.MobUid;
        var placerProto = _placementManager.CurrentPermission?.EntityType;
        var placerIsRCD = HasComp<RCDComponent>(placerEntity);

        // Exit if erasing or the current placer is not an RCD (build mode is active)
        if (_placementManager.Eraser || (placerEntity != null && !placerIsRCD))
            return;

        // Determine if player is carrying an RCD in their active hand
        var player = _playerManager.LocalSession?.AttachedEntity;

        if (!TryComp<HandsComponent>(player, out var hands))
            return;

        var heldEntity = hands.ActiveHand?.HeldEntity;

        if (!TryComp<RCDComponent>(heldEntity, out var rcd))
        {
            // If the player was holding an RCD, but is no longer, cancel placement
            if (placerIsRCD)
                _placementManager.Clear();

            return;
        }

        // Update the direction the RCD prototype based on the placer direction
        if (_placementDirection != _placementManager.Direction)
        {
            _placementDirection = _placementManager.Direction;
            RaiseNetworkEvent(new RCDConstructionGhostRotationEvent(GetNetEntity(heldEntity.Value), _placementDirection));
        }

        // If the placer has not changed, exit
        _rcdSystem.UpdateCachedPrototype(heldEntity.Value, rcd);

        if (heldEntity == placerEntity && rcd.CachedPrototype.Prototype == placerProto)
            return;

        // Create a new placer
        var newObjInfo = new PlacementInformation
        {
            MobUid = heldEntity.Value,
            PlacementOption = _placementMode,
            EntityType = rcd.CachedPrototype.Prototype,
            Range = (int) Math.Ceiling(SharedInteractionSystem.InteractionRange),
            IsTile = (rcd.CachedPrototype.Mode == RcdMode.ConstructTile),
            UseEditorContext = false,
        };

        _placementManager.Clear();
        _placementManager.BeginPlacing(newObjInfo);
    }
}
