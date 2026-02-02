using Content.Shared.Alert;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Adjusts a given alert on this entity.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class AdjustAlertEntityEffectSysten : EntityEffectSystem<AlertsComponent, AdjustAlert>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;

    protected override void Effect(Entity<AlertsComponent> entity, ref EntityEffectEvent<AdjustAlert> args)
    {
        var time = args.Effect.Time;
        var clear = args.Effect.Clear;
        var type = args.Effect.AlertType;

        if (clear && time <= TimeSpan.Zero)
        {
            _alerts.ClearAlert(entity.AsNullable(), type);
        }
        else
        {
            (TimeSpan, TimeSpan)? cooldown = null;

            if ((args.Effect.ShowCooldown || clear) && args.Effect.Time >= TimeSpan.Zero)
                cooldown = (_timing.CurTime, _timing.CurTime + time);

            _alerts.ShowAlert(entity.AsNullable(), type, cooldown: cooldown, autoRemove: clear, showCooldown: args.Effect.ShowCooldown);
        }

    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AdjustAlert : EntityEffectBase<AdjustAlert>
{
    /// <summary>
    /// The specific Alert that will be adjusted
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AlertPrototype> AlertType;

    /// <summary>
    /// If true, the alert is removed after Time seconds. If Time was not specified the alert is removed immediately.
    /// </summary>
    [DataField]
    public bool Clear;

    /// <summary>
    /// Visually display cooldown progress over the alert icon.
    /// </summary>
    [DataField]
    public bool ShowCooldown;

    /// <summary>
    /// The length of the cooldown or delay before removing the alert (in seconds).
    /// </summary>
    [DataField]
    public TimeSpan Time;
}
