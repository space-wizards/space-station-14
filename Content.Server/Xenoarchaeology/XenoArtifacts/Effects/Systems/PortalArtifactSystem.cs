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
        var map = Transform(artifact).MapID);
        var firstPortal = Spawn(artifact.Comp.PortalProto, Transform(artifact).MapPosition);

        var mindQuery = EntityQuery<MindContainerComponent, TransformComponent>().Where((uid, mc, xform) => mc.HasMind && xform.MapID == map).ToList();
        MindContainerComponent? target = null;
        for (int i = 0; i < 50; i++)
        {
            var rndCheck = _random.Pick(mindQuery);

            if (!rndCheck.HasMind) continue;
            if (Transform(rndCheck.Owner).MapID != Transform(artifact).MapID) continue;

            target = rndCheck;
            break;
        }

        if (target == null) return;

        var secondPortal = Spawn(artifact.Comp.PortalProto, Transform(target.Owner).MapPosition);

        _link.TryLink(firstPortal, secondPortal, true);
    }
}
