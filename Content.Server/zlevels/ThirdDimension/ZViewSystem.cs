using Content.Shared._Afterlight.ThirdDimension;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.Server._Afterlight.ThirdDimension;

public sealed class ZViewSystem : SharedZViewSystem
{
    [Dependency] private readonly ViewSubscriberSystem _view = default!;
    [Dependency] private readonly SharedZLevelSystem _zLevel = default!;
    [Dependency] private readonly IServerNetManager _serverNet = default!;

    public override void Initialize()
    {
        base.Initialize();
        _serverNet.Connected += (sender, args) => _zLevel.UpdateMapList();
    }

    public override EntityUid SpawnViewEnt(EntityUid source, MapCoordinates loc)
    {
        var ent = Spawn(null, loc);
        EnsureComp<EyeComponent>(ent);
        var actor = Comp<ActorComponent>(source);
        _view.AddViewSubscriber(ent, actor.PlayerSession);
        return ent;
    }

    public override bool CanSetup(EntityUid source)
    {
        return TryComp<ActorComponent>(source, out var actor) && actor.PlayerSession.AttachedEntity == source;
    }
}
