#region

using Content.Shared.Atmos.AirlockController;
using Content.Shared.Examine;
using Content.Shared.Sound;
using Content.Shared.Sound.Components;
using Robust.Shared.Timing;

#endregion

namespace Content.Server.Atmos.AirlockController.Systems;

/// <summary>
///     Sprite, light, alarm and examine for anything showing an airlock cycle.
///     Shared by controller and its panels.
/// </summary>
public sealed partial class AirlockCycleStatusSystem : EntitySystem
{
    public const int SidePriority = 3;
    public const int StatusPriority = 2;
    public const int StallPriority = 1;

    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedEmitSoundSystem _emitSound = default!;
    [Dependency] private SharedPointLightSystem _pointLight = default!;
    [Dependency] private IGameTiming _timing = default!;

    public void Apply(EntityUid uid, AirlockCycleStatus status)
    {
        var display = status.Maintenance
            ? AirlockControllerDisplay.Maintenance
            : status.Side == AirlockSide.A
                ? AirlockControllerDisplay.SideA
                : AirlockControllerDisplay.SideB;

        _appearance.SetData(uid, AirlockControllerVisuals.State, status.State);
        _appearance.SetData(uid, AirlockControllerVisuals.Display, display);
        _appearance.SetData(uid, AirlockControllerVisuals.Error, status.Stall != null);
        _appearance.SetData(uid, AirlockControllerVisuals.Cycling, status.Warning);

        _pointLight.SetEnabled(uid, status.Warning);

        if (!TryComp<SpamEmitSoundComponent>(uid, out var alarm) || alarm.Enabled == status.Warning)
            return;

        _emitSound.SetEnabled((uid, alarm), status.Warning);

        // SetEnabled pushes the first one a full interval out
        if (status.Warning)
            alarm.NextSound = _timing.CurTime;
    }

    public void Examine(AirlockCycleStatus status, ExaminedEvent args)
    {
        if (status.Maintenance)
        {
            args.PushMarkup(Loc.GetString("airlock-controller-examine-maintenance"), StatusPriority);
            return;
        }

        args.PushMarkup(Loc.GetString("airlock-controller-examine-side",
            ("side", Loc.GetString(AirlockControllerLocale.SideKey(status.Side)))),
            SidePriority);

        args.PushMarkup(Loc.GetString("airlock-controller-examine-state",
            ("state", Loc.GetString(AirlockControllerLocale.StateKey(status.State)))),
            StatusPriority);

        if (status.Stall is { } reason)
        {
            args.PushMarkup(Loc.GetString("airlock-controller-examine-error",
                ("reason", Loc.GetString(AirlockControllerLocale.StallKey(reason)))),
                StallPriority);
        }
    }
}
