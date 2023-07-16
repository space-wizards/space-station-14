using System.Linq;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.ReactionEffects;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Chemistry.Components;
using Robust.Server.GameObjects;
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
        sol.AddReagent(component.SelectedReagent, component.ReagentAmount *
                                                  (component.MinFoamAmount +
                                                   (component.MaxFoamAmount - component.MinFoamAmount) * _random.NextFloat()));

        var foamEnt = Spawn("Foam", xform.Coordinates);
        var smoke = EnsureComp<SmokeComponent>(foamEnt);
        EntityManager.System<SmokeSystem>().Start(foamEnt, smoke, sol, 20f);
    }
}
