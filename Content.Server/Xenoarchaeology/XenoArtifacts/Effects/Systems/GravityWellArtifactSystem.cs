using Content.Server.Singularity.Components;
using Content.Server.Singularity.EntitySystems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
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

        SubscribeLocalEvent<GravityWellArtifactComponent, ComponentStartup>(SetupGravityWell);
        SubscribeLocalEvent<GravityWellArtifactComponent, ComponentShutdown>(ShutdownGravityWell);
    }

    private void SetupGravityWell(EntityUid uid, GravityWellArtifactComponent comp, ComponentStartup args)
    {
        if (HasComp<GravityWellComponent>(uid))
        {
            comp.EntityWasAlreadyAGravityWell = true;
            return;
        }

        var gravWell = AddComp<GravityWellComponent>(uid);
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

    private void ShutdownGravityWell(EntityUid uid, GravityWellArtifactComponent comp, ComponentShutdown args)
    {
        if (comp.EntityWasAlreadyAGravityWell)
            return;

        /// Can't source-track so just assume that we are reponsible for making the entity a gravity well.
        RemComp<GravityWellComponent>(uid);
    }
}
