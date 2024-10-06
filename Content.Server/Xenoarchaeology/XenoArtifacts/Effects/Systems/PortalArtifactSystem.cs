using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Tag;
using Content.Shared.Teleportation.Systems;
using FastAccessors;
using JetBrains.FormatRipper.Elf;
using Microsoft.CodeAnalysis;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices.Marshalling;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class PortalArtifactSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PortalArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(Entity<PortalArtifactComponent> artifact, ref ArtifactActivatedEvent args)
    {
        var map = Transform(artifact).MapID;
        var validMinds = new List<EntityUid>();
        var mindMobs = new HashSet<Entity<MindContainerComponent, TagComponent>>();

        _lookup.GetEntitiesOnMap<MindContainerComponent, TagComponent>(map, mindMobs);
        foreach (var comp in mindMobs)
        {
            // make sure explicitly exclude the ai       
            var tags = new HashSet<ProtoId<TagPrototype>>();
            tags = comp.Comp2.Tags;

            var valid = true;
            foreach (var tag in tags)
            {
                Debug.Print(tag);
                if (tag.Id == "StationAi")
                    valid = false;
            }

            // assumes entity is in a container if the local position is EXACTLY 0,0 (if not, lucky you)
            if (Transform(comp).LocalPosition == new Vector2(0.0f, 0.0f))
                valid = false;

            // check if the first component (the MindContainer) has a Mind
            if (comp.Comp1.HasMind && valid)
            {
                validMinds.Add(comp.Owner);
            }
        }

        //this would only be 0 if there were a station full of AIs and no one else
        if (validMinds.Count != 0)
        {
            var firstPortal = Spawn(artifact.Comp.PortalProto, _transform.GetMapCoordinates(artifact));

            var target = _random.Pick(validMinds);

            var secondPortal = Spawn(artifact.Comp.PortalProto, _transform.GetMapCoordinates(target));

            //Manual position swapping, because the portal that opens doesn't trigger a collision, and doesn't teleport targets the first time.
            _transform.SwapPositions(target, secondPortal);

            _link.TryLink(firstPortal, secondPortal, true);
        }
        else
        {
            _adminLogger.Add(LogType.Teleport, $"Portal Artifact {ToPrettyString(artifact.Owner)} failed to find valid Teleport target");
        }
    }
}
