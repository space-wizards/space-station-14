using Content.Server.Fluids.EntitySystems;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

public sealed class XAEFoamSystem : BaseXAESystem<XAEFoamComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XAEFoamComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, XAEFoamComponent component, MapInitEvent args)
    {
        if (component.SelectedReagent != null)
            return;

        if (component.Reagents.Count == 0)
            return;

        component.SelectedReagent = _random.Pick(component.Reagents);
    }

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEFoamComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var component = ent.Comp;
        if (component.SelectedReagent == null)
            return;

        var sol = new Solution();
        var xform = Transform(ent.Owner);
        var range = (int)MathF.Round(MathHelper.Lerp(component.MinFoamAmount, component.MaxFoamAmount, _random.NextFloat(0, 1f)));
        sol.AddReagent(component.SelectedReagent, component.ReagentAmount);
        var foamEnt = Spawn("Foam", xform.Coordinates);
        var spreadAmount = range * 4;
        _smoke.StartSmoke(foamEnt, sol, component.Duration, spreadAmount);
    }
}
