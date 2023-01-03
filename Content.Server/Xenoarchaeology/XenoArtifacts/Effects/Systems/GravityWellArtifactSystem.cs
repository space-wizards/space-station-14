using Content.Server.Singularity.Components;
using Content.Server.Singularity.EntitySystems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class GravityWellArtifactSystem : EntitySystem
{
    #region Dependencies
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GravityWellSystem _gravWellSystem = default!;
    #endregion Dependencies

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GravityWellArtifactComponent, ArtifactNodeEnteredEvent>(SetupGravityWell);
    }

    /// <summary>
    /// Sets up the state of the <see cref="GravityWellComponent"/> involved in granting an artifact a gravity well as an effect.
    /// </summary>
    /// <param name="uid">The uid of the artifact being given a gravity well.</param>
    /// <param name="comp">The state of the gravity well effect the artifact has.</param>
    /// <param name="args">The prompt to initialize the effects of the current node.</param>
    private void SetupGravityWell(EntityUid uid, GravityWellArtifactComponent comp, ArtifactNodeEnteredEvent args)
    {
        GravityWellComponent? gravWell = null;
        if(!Resolve(uid, ref gravWell))
            return;

        gravWell.MinRange = _random.NextFloat(comp.MinRange.min, comp.MinRange.max);
        gravWell.MaxRange = _random.NextFloat(comp.MaxRange.min, comp.MaxRange.max);
        gravWell.BaseRadialAcceleration = _random.NextFloat(comp.RadialAcceleration.min, comp.RadialAcceleration.max);
        gravWell.BaseTangentialAcceleration = _random.NextFloat(comp.TangentialAcceleration.min, comp.TangentialAcceleration.max);
        _gravWellSystem.SetPulsePeriod(
            uid,
            TimeSpan.FromSeconds(_random.NextFloat((float)comp.PulsePeriod.min.TotalSeconds, (float)comp.PulsePeriod.max.TotalSeconds)),
            gravWell
        );
    }
}
