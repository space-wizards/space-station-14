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

public sealed class CloudEmoteSystem : SharedCloudEmoteSystem // Ideally better to be replaced to Control, like SpeechBubble.cs
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    private ISawmill _sawmill = default!;
    private static readonly float GAP_SIZE = 0.3f;
    public CloudEmoteSystem()
    {
        _sawmill = Logger.GetSawmill("cloud_emotes");
    }

    public override void FrameUpdate(float frameTime)
    {
        var query = _entityManager.EntityQueryEnumerator<CloudEmoteActiveComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!Exists(uid) || !(Exists(comp.Emote))) continue;
            update_position(uid, comp.Emote);
        }
    }

    private void update_position(EntityUid player, EntityUid emote)
    {
        var position = _transformSystem.GetWorldPosition(player);
        var playerHeight = _entityManager.GetComponent<SpriteComponent>(player).Bounds.Height;
        
        /* Adds camera rotation vector so no matter how you rotate camera it always stays. */
        var offset = (-_eyeManager.CurrentEye.Rotation).ToWorldVec();
        /* Multiplies by half of player height (because player's center is 0,0) and by GAP_SIZE */
        offset *= -(playerHeight/2 + GAP_SIZE);
        var rotation = _transformSystem.GetWorldRotation(player); // Prototype dictates that only SpriteComponent automatically rotates, but not TransformComponent
        _transformSystem.SetWorldPositionRotation(emote, position+offset, rotation);
    }    
}
