using Content.Shared.CCVar;
using Content.Shared.Popups;
using Content.Shared.Shuttles.Components;
using Content.Shared.UserInterface;
using Robust.Shared.Configuration;

namespace Content.Shared.Shuttles.Systems;

public abstract partial class SharedEmergencyShuttleSystem : EntitySystem
{
    [Dependency] protected IConfigurationManager ConfigManager = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;

    private bool _emergencyEarlyLaunchAllowed;

    /// <summary>
    /// Has the emergency shuttle arrived?
    /// </summary>
    public bool EmergencyShuttleArrived { get; protected set; }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, ActivatableUIOpenAttemptEvent>(OnEmergencyOpenAttempt);

        Subs.CVar(ConfigManager, CCVars.EmergencyEarlyLaunchAllowed, value => _emergencyEarlyLaunchAllowed = value, true);
    }

    private void OnEmergencyOpenAttempt(Entity<EmergencyShuttleConsoleComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        // I'm hoping ActivatableUI checks it's open before allowing these messages.
        if (_emergencyEarlyLaunchAllowed)
            return;

        args.Cancel();

        if (!args.Silent)
            Popup.PopupEntity(Loc.GetString("emergency-shuttle-console-no-early-launches"), ent, args.User);
    }

    /// <summary>
    ///     Attempts to get the EntityUid of the emergency shuttle
    /// </summary>
    public EntityUid? GetShuttle()
    {
        AllEntityQuery<EmergencyShuttleComponent>().MoveNext(out var shuttle, out _);
        return shuttle;
    }
}
