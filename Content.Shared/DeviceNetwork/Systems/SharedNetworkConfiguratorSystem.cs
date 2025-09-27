using Content.Shared.Actions;
using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.DeviceNetwork.Systems;

public abstract class SharedNetworkConfiguratorSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        //Interaction
        SubscribeLocalEvent<NetworkConfiguratorComponent, ActivatableUIOpenAttemptEvent>(OnUiOpenAttempt);
        SubscribeLocalEvent<NetworkConfiguratorComponent, AfterInteractEvent>(AfterInteract); //TODO: Replace with utility verb?
        SubscribeLocalEvent<NetworkConfiguratorComponent, ExaminedEvent>(DoExamine);

        //Verbs
        SubscribeLocalEvent<NetworkConfiguratorComponent, GetVerbsEvent<UtilityVerb>>(OnAddInteractVerb);
        SubscribeLocalEvent<DeviceNetworkComponent, GetVerbsEvent<AlternativeVerb>>(OnAddAlternativeSaveDeviceVerb);
        SubscribeLocalEvent<NetworkConfiguratorComponent, GetVerbsEvent<AlternativeVerb>>(OnAddSwitchModeVerb);
    }

    #region Interaction

    private void OnUiOpenAttempt(Entity<NetworkConfiguratorComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (ent.Comp.LinkModeActive)
            args.Cancel();
    }

    private void DoExamine(Entity<NetworkConfiguratorComponent> ent, ref ExaminedEvent args)
    {
        var mode = ent.Comp.LinkModeActive ? "network-configurator-examine-mode-link" : "network-configurator-examine-mode-list";
        args.PushMarkup(Loc.GetString("network-configurator-examine-current-mode", ("mode", Loc.GetString(mode))));
    }

    private void AfterInteract(Entity<NetworkConfiguratorComponent> ent, ref AfterInteractEvent args)
    {
        OnUsed(ent, args.Target, args.User, args.CanReach);
    }

    #endregion
    #region Verbs

    /// <summary>
    /// Adds the interaction verb which is either configuring device lists or saving a device onto the configurator
    /// </summary>
    private void OnAddInteractVerb(Entity<NetworkConfiguratorComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.Using.HasValue)
            return;

        var user = args.User;
        var target = args.Target;
        var verb = new UtilityVerb
        {
            Act = () => OnUsed(ent, target, user),
            Impact = LogImpact.Low
        };

        if (ent.Comp.LinkModeActive && (HasComp<DeviceLinkSinkComponent>(target) || HasComp<DeviceLinkSourceComponent>(target)))
        {
            var linkStarted = ent.Comp.ActiveDeviceLink.HasValue;
            verb.Text = Loc.GetString(linkStarted ? "network-configurator-link" : "network-configurator-start-link");
            verb.Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/in.svg.192dpi.png"));
            args.Verbs.Add(verb);
        }
        else if (HasComp<DeviceNetworkComponent>(target))
        {
            var isDeviceList = HasComp<DeviceListComponent>(target);
            verb.Text = Loc.GetString(isDeviceList ? "network-configurator-configure" : "network-configurator-save-device");
            verb.Icon = isDeviceList
                ? new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/settings.svg.192dpi.png"))
                : new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/in.svg.192dpi.png"));
            args.Verbs.Add(verb);
        }
    }

    /// <summary>
    /// Powerful. Funny alt interact using.
    /// Adds an alternative verb for saving a device on the configurator for entities with the <see cref="DeviceListComponent"/>.
    /// Allows alt clicking entities with a network configurator that would otherwise trigger a different action like entities
    /// with a <see cref="DeviceListComponent"/>
    /// </summary>
    private void OnAddAlternativeSaveDeviceVerb(Entity<DeviceNetworkComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.Using.HasValue
            || !TryComp<NetworkConfiguratorComponent>(args.Using.Value, out var configurator))
            return;

        var target = args.Target;
        var used = args.Using.Value;
        var user = args.User;
        if (!configurator.LinkModeActive && HasComp<DeviceListComponent>(target))
        {
            AlternativeVerb verb = new()
            {
                Text = Loc.GetString("network-configurator-save-device"),
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/in.svg.192dpi.png")),
                Act = () => TryAddNetworkDevice(used, target, user, configurator: configurator),
                Impact = LogImpact.Low
            };
            args.Verbs.Add(verb);
            return;
        }

        if (configurator is { LinkModeActive: true, ActiveDeviceLink: { } }
        && (HasComp<DeviceLinkSinkComponent>(target) || HasComp<DeviceLinkSourceComponent>(target)))
        {
            AlternativeVerb verb = new()
            {
                Text = Loc.GetString("network-configurator-link-defaults"),
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/in.svg.192dpi.png")),
                Act = () => TryLinkDefaults(used, configurator, target, user),
                Impact = LogImpact.Low
            };
            args.Verbs.Add(verb);
        }
    }

    private void OnAddSwitchModeVerb(Entity<NetworkConfiguratorComponent> configurator, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.Using.HasValue || !HasComp<NetworkConfiguratorComponent>(args.Target))
            return;

        var user = args.User;
        var target = args.Target;
        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("network-configurator-switch-mode"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
            Act = () => SwitchMode(configurator, user),
            Impact = LogImpact.Low
        };
        args.Verbs.Add(verb);
    }

    #endregion

    /// <summary>
    /// Either adds a device to the device list or shows the config ui if the target is ant entity with a device list
    /// </summary>
    private void OnUsed(Entity<NetworkConfiguratorComponent> configurator, EntityUid? target, EntityUid user, bool canReach = true)
    {
        if (!canReach || !target.HasValue)
            return;

        DetermineMode(configurator, target, user);

        if (configurator.Comp.LinkModeActive)
        {
            TryLinkDevice(configurator.Owner, configurator.Comp, target, user);
            return;
        }

        if (!HasComp<DeviceListComponent>(target))
        {
            TryAddNetworkDevice(configurator.Owner, target, user, configurator.Comp);
            return;
        }

        OpenDeviceListUi(configurator.Owner, target, user, configurator.Comp);
    }

    private void DetermineMode(Entity<NetworkConfiguratorComponent> configurator, EntityUid? target, EntityUid user)
    {
        var hasLinking = HasComp<DeviceLinkSinkComponent>(target) || HasComp<DeviceLinkSourceComponent>(target);

        if (hasLinking && HasComp<DeviceListComponent>(target) || hasLinking == configurator.Comp.LinkModeActive)
            return;

        if (hasLinking)
        {
            SetMode(configurator, user, true);
            return;
        }

        if (HasComp<DeviceNetworkComponent>(target))
            SetMode(configurator, user, false);
    }

    /// <summary>
    /// Toggles between linking and listing mode
    /// </summary>
    private void SwitchMode(Entity<NetworkConfiguratorComponent> configurator, EntityUid user)
    {
        if (Delay(configurator))
            return;

        configurator.Comp.LinkModeActive = !configurator.Comp.LinkModeActive;

        if (!configurator.Comp.LinkModeActive)
            configurator.Comp.ActiveDeviceLink = null;

        UpdateModeAppearance(configurator, user);
    }

    /// <summary>
    /// Sets the mode to linking or list depending on the link mode parameter
    /// </summary>>
    private void SetMode(Entity<NetworkConfiguratorComponent> configurator, EntityUid user, bool linkMode)
    {
        configurator.Comp.LinkModeActive = linkMode;

        if (!linkMode)
            configurator.Comp.ActiveDeviceLink = null;

        UpdateModeAppearance(configurator, user);
    }

    /// <summary>
    /// Updates the configurators appearance and plays a sound indicating that the mode switched
    /// </summary>
    private void UpdateModeAppearance(Entity<NetworkConfiguratorComponent> configurator, EntityUid user)
    {
        Dirty(configurator);
        _appearanceSystem.SetData(configurator.Owner, NetworkConfiguratorVisuals.Mode, configurator.Comp.LinkModeActive);

        var pitch = configurator.Comp.LinkModeActive ? 1 : 0.8f;
        _audioSystem.PlayPredicted(configurator.Comp.SoundSwitchMode, configurator.Owner, user, AudioParams.Default.WithVolume(1.5f).WithPitchScale(pitch));
    }

    /// <summary>
    /// Returns true if the last time this method was called is earlier than the configurators use delay.
    /// </summary>
    protected bool Delay(Entity<NetworkConfiguratorComponent> ent)
    {
        var currentTime = _gameTiming.CurTime;
        if (currentTime < ent.Comp.LastUseAttempt + ent.Comp.UseDelay)
            return true;

        ent.Comp.LastUseAttempt = currentTime;
        Dirty(ent);
        return false;
    }

    protected virtual void TryAddNetworkDevice(
        EntityUid configuratorUid,
        EntityUid? targetUid,
        EntityUid userUid,
        NetworkConfiguratorComponent? configurator = null,
        DeviceNetworkComponent? device = null)
    { }

    protected virtual void TryLinkDevice(
        EntityUid uid,
        NetworkConfiguratorComponent configurator,
        EntityUid? target,
        EntityUid user)
    { }

    protected virtual void TryLinkDefaults(
        EntityUid configuratorUid,
        NetworkConfiguratorComponent configurator,
        EntityUid? targetUid,
        EntityUid user)
    { }

    protected virtual void OpenDeviceLinkUi(
        EntityUid configuratorUid,
        EntityUid? targetUid,
        EntityUid userUid,
        NetworkConfiguratorComponent configurator)
    { }

    protected virtual void OpenDeviceListUi(
        EntityUid configuratorUid,
        EntityUid? targetUid,
        EntityUid userUid,
        NetworkConfiguratorComponent configurator)
    { }
}

public sealed partial class ClearAllOverlaysEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public enum NetworkConfiguratorVisuals
{
    Mode
}

[Serializable, NetSerializable]
public enum NetworkConfiguratorLayers
{
    ModeLight
}
