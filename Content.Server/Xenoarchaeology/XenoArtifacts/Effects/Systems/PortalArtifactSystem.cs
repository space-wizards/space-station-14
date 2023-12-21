using System.Linq;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Teleportation.Systems;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class PortalArtifactSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly LinkedEntitySystem _link = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PortalArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(Entity<PortalArtifactComponent> artifact, ref ArtifactActivatedEvent args)
    {
        var map = Transform(artifact).MapID;
        var firstPortal = Spawn(artifact.Comp.PortalProto, Transform(artifact).MapPosition);

        var mindQuery = EntityQueryEnumerator<MindContainerComponent, TransformComponent>();
        while (mindQuery.MoveNext(out var uid, out var mind))
        {
            
        }

        var target = _random.Pick(mindQuery);
        var secondPortal = Spawn(artifact.Comp.PortalProto, Transform(target.Owner).MapPosition);

        _link.TryLink(firstPortal, secondPortal, true);

        //var query = EntityQueryEnumerator();
        //while (query.MoveNext(out var uid, out var egg))
        //{
        //    Hatch(uid, egg);
        //}
        //.Where((uid, mc, xform) => mc.HasMind && xform.MapID == map)
    }
}
