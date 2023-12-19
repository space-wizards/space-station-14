using System.Linq;
using System.Numerics;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Mind;
using Content.Shared.Storage;
using Content.Shared.Teleportation.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
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
        var firstPortal = Spawn(artifact.Comp.PortalProto, Transform(artifact.Owner).MapPosition);

        var mindQuery = EntityQuery<MindComponent>().ToList();
        var target = _random.Pick(mindQuery);

        var secondPortal = Spawn(artifact.Comp.PortalProto, Transform(target.Owner).MapPosition);

        _link.TryLink(firstPortal, secondPortal, true);
    }
}
