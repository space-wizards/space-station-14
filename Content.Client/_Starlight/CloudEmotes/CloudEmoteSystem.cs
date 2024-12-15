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
            _sawmill.Info(comp.Phase.ToString());
        }
    }

    private void update_position(EntityUid player, EntityUid emote)
    {
        if (!Exists(emote))
        {
            
            return;
        }
        var position = _transformSystem.GetWorldPosition(player);
        _transformSystem.SetWorldPosition(emote, position);
    }            
}
