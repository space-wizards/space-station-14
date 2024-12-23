using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Server.Revenant.EntitySystems;
using Content.Shared.Item;
using System.Linq;
using Robust.Shared.Random;
using Robust.Shared.Containers;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class AnimateArtifactSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RevenantAnimatedSystem _revenantAnimated = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;


    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<AnimateArtifactComponent, ArtifactActivatedEvent>(OnActivated);
    }

    private void OnActivated(EntityUid uid, AnimateArtifactComponent component, ArtifactActivatedEvent args)
    {
        // Get a list of all nearby objects in range

        var entsHash = _lookup.GetEntitiesInRange(uid, component.Range);
        entsHash.Add(uid);
        var numSuccessfulAnimates = 0;

        var unshuffledEnts = entsHash.ToList();
        var ents = unshuffledEnts.OrderBy(_ => _random.Next()).ToList();

        foreach (var ent in ents)
        {
            if (numSuccessfulAnimates >= component.Count)
            {
                break;
            }
            // need to only get items not in a container
            if (HasComp<ItemComponent>(ent) && _revenantAnimated.CanAnimateObject(ent) && !_container.IsEntityInContainer(ent))
            {
                if (_revenantAnimated.TryAnimateObject(ent, TimeSpan.FromSeconds(component.Duration)))
                {
                    numSuccessfulAnimates += 1;
                }
            }
        }
    }
}
