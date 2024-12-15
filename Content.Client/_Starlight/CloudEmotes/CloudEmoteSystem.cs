namespace Content.Client._Starlight.CloudEmotes;
using Content.Client.Movement.Systems;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Content.Shared.Eye.Blinding;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared._Starlight.CloudEmote;
using Robust.Client.GameObjects;
using Robust.Shared.Map;

public sealed class CloudEmoteSystem : SharedCloudEmoteSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    private ISawmill _sawmill = default!;

    public CloudEmoteSystem()
    {
        _sawmill = Logger.GetSawmill("cloud_emotes");
    }

    public override void FrameUpdate(float frameTime)
    {
        var query = _entityManager.EntityQueryEnumerator<CloudEmoteActiveComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            update_position(uid, comp.Emote);
        }
    }

    private void update_position(EntityUid player, EntityUid emote)
    {
        var position = _transformSystem.GetWorldPosition(player);
        _sawmill.Info("player.position" + position.ToString());
        var offset = _entityManager.GetComponent<SpriteComponent>(player).Bounds.Height;
      //  _transformSystem.SetCoordinates(emote, new EntityCoordinates(player, 0, offset));
        var rotation = _transformSystem.GetWorldRotation(player); // Prototype dictates that only SpriteComponent automatically rotates, but not TransformComponent
        _sawmill.Info("position set to"+ position.ToString());
        _transformSystem.SetWorldPositionRotation(emote, position, rotation);
    }    
}
