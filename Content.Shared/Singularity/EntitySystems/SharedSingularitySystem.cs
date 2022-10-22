using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

using Content.Shared.Radiation.Components;
using Content.Shared.Singularity;
using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.Events;

namespace Content.Shared.Singularity.EntitySystems;

public abstract class SharedSingularitySystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _visualizer = default!;
    [Dependency] private readonly SharedEventHorizonSystem _horizons = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public const ulong MinSingularityLevel = 0;
    public const ulong MaxSingularityLevel = 6;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AppearanceComponent, SingularityLevelChangedEvent>(UpdateAppearance);
        SubscribeLocalEvent<RadiationSourceComponent, SingularityLevelChangedEvent>(UpdateRadiation);
        SubscribeLocalEvent<PhysicsComponent, SingularityLevelChangedEvent>(UpdateBody);
        SubscribeLocalEvent<SharedEventHorizonComponent, SingularityLevelChangedEvent>(UpdateEventHorizon);
        SubscribeLocalEvent<SingularityDistortionComponent, SingularityLevelChangedEvent>(UpdateDistortion);
    }

#region Getters/Setters

    public float GravPulseRange(SharedSingularityComponent singulo)
        => 2f * (singulo.Level + 1);

    public (float, float) GravPulseAcceleration(SharedSingularityComponent singulo)
        => (10f * singulo.Level, 0f);

    public float EventHorizonRadius(SharedSingularityComponent singulo)
        => (float) singulo.Level - 0.5f;

    public bool CanBreachContainment(SharedSingularityComponent singulo)
        => singulo.Level > 4;

    public float GetFalloff(float level)
    {
        return level switch {
            0 => 9999f,
            1 => MathF.Sqrt(6.4f),
            2 => MathF.Sqrt(7.0f),
            3 => MathF.Sqrt(8.0f),
            4 => MathF.Sqrt(10.0f),
            5 => MathF.Sqrt(12.0f),
            6 => MathF.Sqrt(12.0f),
            _ => -1.0f
        };
    }

    public float GetIntensity(float level)
    {
        return level switch {
            0 => 0.0f,
            1 => 3645f,
            2 => 103680f,
            3 => 1113920f,
            4 => 16200000f,
            5 => 180000000f,
            6 => 180000000f,
            _ => -1.0f
        };
    }

    /// <summary>
    ///
    /// </summary>
    public void SetSingularityLevel(SharedSingularityComponent singularity, ulong value)
    {
        var oldValue = singularity._level;
        if (oldValue == value)
            return;

        singularity._level = value;
        UpdateSingularityLevel(singularity, oldValue);
        if(!singularity.Deleted)
            singularity.Dirty();
    }

    public void UpdateSingularityLevel(SharedSingularityComponent singularity, ulong oldValue)
    {
        RaiseLocalEvent(singularity.Owner, new SingularityLevelChangedEvent(singularity.Level, oldValue, singularity, this));
        if (singularity.Level <= 0)
            EntityManager.DeleteEntity(singularity.Owner);
    }

    public void UpdateSingularityLevel(SharedSingularityComponent singularity)
        => UpdateSingularityLevel(singularity, singularity.Level);

#endregion Getters/Setters

#region EventHandlers
    // TODO: Figure out which systems should have control of which coupling.
    private void UpdateEventHorizon(EntityUid uid, SharedEventHorizonComponent comp, SingularityLevelChangedEvent args)
    {
        _horizons.SetRadius(uid, EventHorizonRadius(args.Singularity), false, comp);
        _horizons.SetCanBreachContainment(uid, CanBreachContainment(args.Singularity), false, comp);
        _horizons.UpdateEventHorizonFixture(uid, comp, null);
    }

    private void UpdateDistortion(EntityUid uid, SingularityDistortionComponent comp, SingularityLevelChangedEvent args)
    {
        comp.FalloffPower = GetFalloff(args.NewValue);
        comp.Intensity = GetIntensity(args.NewValue);
    }

    private void UpdateBody(EntityUid uid, PhysicsComponent comp, SingularityLevelChangedEvent args)
    {
        comp.BodyStatus = (args.NewValue > 1) ? BodyStatus.InAir : BodyStatus.OnGround;
        if (args.NewValue <= 1 && args.OldValue > 1)
            _physics.SetLinearVelocity(comp, Vector2.Zero);
    }

    private void UpdateAppearance(EntityUid uid, AppearanceComponent comp, SingularityLevelChangedEvent args)
    {
        _visualizer.SetData(uid, SingularityVisuals.Level, args.NewValue, comp);
    }

    private void UpdateRadiation(EntityUid uid, RadiationSourceComponent comp, SingularityLevelChangedEvent args)
    {
        comp.Intensity = args.Singularity.RadsPerLevel * args.NewValue;
    }

#endregion EventHandlers

}
