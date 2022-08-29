using Content.Shared.Alert;
using Content.Shared.Movement.Systems;

namespace Content.Server.Traits.Smoker;

public sealed class SmokerTraitSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedJetpackSystem _jetpack = default!;

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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulatedFrameTime += frameTime;

        if (_accumulatedFrameTime > 1)
        {
            foreach (var component in EntityManager.EntityQuery<SmokerTraitComponent>())
            {
                UpdateCraving(component, 1f);
            }

            _accumulatedFrameTime -= 1;
        }
    }

    /// <summary>
    ///     Adds craving to the component. Can be negative to remove craving.
    /// </summary>
    public void UpdateCraving(SmokerTraitComponent component, float amount)
    {
        component.CurrentCraving = Math.Clamp(component.CurrentCraving + amount, 0f, MaxCraving);

        var threshold = GetThreshold(component.CurrentCraving);
        if (threshold != component.CurrentThreshold)
        {
            component.CurrentThreshold = threshold;
            UpdateEffects(component);
        }
    }

    /// <summary>
    ///     Resets craving to zero.
    /// </summary>
    public void ResetCraving(SmokerTraitComponent component)
    {
        component.CurrentCraving = 0f;
        UpdateEffects(component);
    }

    private void OnComponentStartup(EntityUid uid, SmokerTraitComponent component, ComponentStartup args)
    {
        component.CurrentCraving = 0f;
        component.CurrentThreshold = GetThreshold(component.CurrentCraving);
        UpdateEffects(component);
    }

    private void OnRefreshMovespeed(EntityUid uid, SmokerTraitComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        // TODO: This should really be taken care of somewhere else
        if (_jetpack.IsUserFlying(component.Owner))
            return;

        var mod = component.CurrentThreshold == CravingThreshold.Intense ? 0.9f : 1.0f;
        args.ModifySpeed(mod, mod);
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
