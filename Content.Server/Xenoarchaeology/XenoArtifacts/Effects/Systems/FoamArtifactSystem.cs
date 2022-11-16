using System.Linq;
using Content.Server.Chemistry.ReactionEffects;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class FoamArtifactSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<FoamArtifactComponent, ArtifactNodeEnteredEvent>(OnNodeEntered);
        SubscribeLocalEvent<FoamArtifactComponent, ArtifactActivatedEvent>(OnActivated);
    }

    private void OnNodeEntered(EntityUid uid, FoamArtifactComponent component, ArtifactNodeEnteredEvent args)
    {
        if (!component.Reagents.Any())
            return;

        component.SelectedReagent = component.Reagents[args.RandomSeed % component.Reagents.Count];
    }

    private void OnActivated(EntityUid uid, FoamArtifactComponent component, ArtifactActivatedEvent args)
    {
        if (component.SelectedReagent == null)
            return;

        var sol = new Solution();
        var xform = Transform(uid);
        sol.AddReagent(component.SelectedReagent, component.ReagentAmount);

        FoamAreaReactionEffect.SpawnFoam("Foam", xform.Coordinates, sol,
            _random.Next(component.MinFoamAmount, component.MaxFoamAmount), component.Duration,
            component.SpreadDuration, component.SpreadDuration, entityManager: EntityManager);
    }
}
