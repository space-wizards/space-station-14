using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.APC;

/// <summary>
/// A system for interactions with APCs.
/// APCs are wall-mounted battery banks and breakers for stepping down MV to LV power.
/// </summary>
/// <seealso cref="ApcComponent"/>
public abstract partial class ApcSystem : EntitySystem
{
    [Dependency] private AccessReaderSystem _accessReader = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnToggleMainBreaker(Entity<ApcComponent> ent, ref ApcToggleMainBreakerMessage args)
    {
        TryToggleMainBreaker(ent, args.Actor);

        UpdateUI(ent);
    }

    private void TryToggleMainBreaker(Entity<ApcComponent> ent, EntityUid user)
    {
        var attemptEv = new ApcToggleMainBreakerAttemptEvent();
        RaiseLocalEvent(ent, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            _popup.PopupCursor(Loc.GetString("apc-component-on-toggle-cancel"), user, PopupType.Medium);
            return;
        }

        if (!_accessReader.IsAllowed(user, ent))
        {
            _popup.PopupClient(Loc.GetString("apc-component-insufficient-access"), ent, user, PopupType.Medium);
            return;
        }

        ApcToggleBreaker(ent.AsNullable(), user);
    }

    /// <summary>
    /// Toggles the enabled state of the APC's main breaker.
    /// </summary>
    public void ApcToggleBreaker(Entity<ApcComponent?> ent, EntityUid? user = null)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.MainBreakerEnabled = !ent.Comp.MainBreakerEnabled;
        Dirty(ent);

        if (ent.Comp.MainBreakerEnabled)
            ent.Comp.TripFlag = false;

        var audioParams = ent.Comp.OnReceiveMessageSound.Params.AddVolume(-2f);
        _audio.PlayPredicted(ent.Comp.OnReceiveMessageSound, ent, user, audioParams);

        if (user != null)
        {
            var humanReadableState = ent.Comp.MainBreakerEnabled ? "Enabled" : "Disabled";
            _adminLogger.Add(LogType.ItemConfigure, LogImpact.Medium,
                $"{ToPrettyString(user):user} set the main breaker state of {ToPrettyString(ent):entity} to {humanReadableState:state}.");
        }

        BreakerToggled((ent, ent.Comp));
    }

    protected virtual void BreakerToggled(Entity<ApcComponent> ent)
    {
    }

    protected abstract void UpdateUI(Entity<ApcComponent> ent);
}

/// <summary>
/// A cancellable event raised on the Apc to check if the main breaker can be toggled by a user.
/// </summary>
[ByRefEvent]
public record struct ApcToggleMainBreakerAttemptEvent(bool Cancelled);
