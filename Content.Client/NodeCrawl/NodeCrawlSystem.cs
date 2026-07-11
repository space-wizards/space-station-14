using Content.Client.SubFloor;
using Content.Shared.NodeCrawl;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.NodeCrawl;

public sealed partial class NodeCrawlSystem : SharedNodeCrawlSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SubFloorHideSystem _subfloor = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NodeCrawlerComponent, LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<NodeCrawlerComponent, LocalPlayerDetachedEvent>(OnDetached);
        SubscribeLocalEvent<NodeCrawlerComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnAttached(Entity<NodeCrawlerComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        if (ent.Comp.Mover is not null)
            _subfloor.Types = ent.Comp.RevealedComponents;
    }

    private void OnDetached(Entity<NodeCrawlerComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _subfloor.Types = new Type[] { };
    }

    private void OnAfterAutoHandleState(Entity<NodeCrawlerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_player.LocalEntity != ent)
            return;

        if (ent.Comp.Mover is not null)
            _subfloor.Types = ent.Comp.RevealedComponents;
        else
            _subfloor.Types = new Type[] { };
    }
}
