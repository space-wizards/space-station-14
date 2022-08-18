using Content.Server.Body.Components;
using Content.Shared.Alert;
using Content.Shared.Movement.Systems;

namespace Content.Server.Traits.Smoker;

public sealed class SmokerTraitSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;

    private float _accumulatedFrameTime;

    private const float MaxCraving = 900f; // After 15 minutes
    private const float IntenseThreshold = 600f; // 10 minutes
    private const float LightThreshold = 360f; // 6 minutes

    private static readonly Dictionary<CravingThreshold, AlertType> CravingThresholdAlertTypes = new()
    {
        { CravingThreshold.Light, AlertType.LightNicotineCraving },
        { CravingThreshold.Intense, AlertType.IntenseNicotineCraving },
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SmokerTraitComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<SmokerTraitComponent, ComponentStartup>(OnComponentStartup);
    }
    private void OnComponentStartup(EntityUid uid, SmokerTraitComponent component, ComponentStartup args)
    {
        component.CurrentCraving = 0f;
        component.CurrentThreshold = GetThreshold(component.CurrentCraving);
        UpdateEffects(component);
    }

    private static void OnRefreshMovespeed(EntityUid uid, SmokerTraitComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        var mod = component.CurrentThreshold == CravingThreshold.Intense ? 0.85f : 1.0f;
        args.ModifySpeed(mod, mod); // Walking speed can just remain the same
    }

    private void UpdateEffects(SmokerTraitComponent component)
    {
        _movement.RefreshMovementSpeedModifiers(component.Owner);

        // Update alerts
        if (CravingThresholdAlertTypes.TryGetValue(component.CurrentThreshold, out var alertId))
            _alerts.ShowAlert(component.Owner, alertId);
        else
            _alerts.ClearAlertCategory(component.Owner, AlertCategory.NicotineCraving);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulatedFrameTime += frameTime;

        if (_accumulatedFrameTime > 1)
        {
            foreach (var component in EntityManager.EntityQuery<SmokerTraitComponent>())
            {
                if (!EntityManager.TryGetComponent<BloodstreamComponent>(component.Owner, out var bloodstream))
                {
                    Logger.Debug("Failed to get BloodstreamComponent");
                }
                else
                {
                    Logger.Debug("--------------------------------------------");
                    foreach (var reagent in bloodstream.BloodSolution.Contents)
                    {
                        Logger.Debug($"{reagent.ReagentId}: {reagent.Quantity}");
                    }
                }

                component.CurrentCraving += 1f;
                component.CurrentCraving = Math.Min(component.CurrentCraving, MaxCraving); // Limit craving

                var threshold = GetThreshold(component.CurrentCraving);
                if (threshold != component.CurrentThreshold)
                {
                    component.CurrentThreshold = threshold;
                    UpdateEffects(component);
                }
            }
            _accumulatedFrameTime -= 1;
        }
    }

    private static CravingThreshold GetThreshold(float amount)
    {
        return amount switch
        {
            >= IntenseThreshold => CravingThreshold.Intense,
            >= LightThreshold => CravingThreshold.Light,
            _ => CravingThreshold.None,
        };
    }
}
