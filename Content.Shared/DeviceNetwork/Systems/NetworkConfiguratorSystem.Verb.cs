using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.DeviceNetwork.Systems;

public sealed partial class NetworkConfiguratorSystem
{
    private void InitializeVerb()
    {
        SubscribeLocalEvent<NetworkConfiguratorComponent, AfterInteractEvent>(AfterInteract); //TODO: Replace with utility verb?
        SubscribeLocalEvent<NetworkConfiguratorComponent, ExaminedEvent>(DoExamine);

        SubscribeLocalEvent<NetworkConfiguratorComponent, GetVerbsEvent<UtilityVerb>>(OnAddInteractVerb);
        SubscribeLocalEvent<DeviceNetworkComponent, GetVerbsEvent<AlternativeVerb>>(OnAddAlternativeSaveDeviceVerb);
        SubscribeLocalEvent<NetworkConfiguratorComponent, GetVerbsEvent<AlternativeVerb>>(OnAddSwitchModeVerb);
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
            TryLinkDevice(configurator, target, user);
            return;
        }

        if (!HasComp<DeviceListComponent>(target))
        {
            TryAddNetworkDevice(configurator.AsNullable(), target.Value, user);
            return;
        }

        OpenDeviceListUi(configurator, target, user, configurator);
    }

    private void DetermineMode(Entity<NetworkConfiguratorComponent> configurator, EntityUid? target, EntityUid userUid)
    {
        var hasLinking = HasComp<DeviceLinkSinkComponent>(target) || HasComp<DeviceLinkSourceComponent>(target);

        if (hasLinking && HasComp<DeviceListComponent>(target) || hasLinking == configurator.Comp.LinkModeActive)
            return;

        var hasNetworking = HasComp<DeviceNetworkComponent>(target);
        if (hasNetworking)
            SetMode(configurator, userUid, false);
        else if (hasLinking)
            SetMode(configurator, userUid, true);
    }

    /// <summary>
    /// Adds the interaction verb which is either configuring device lists or saving a device onto the configurator
    /// </summary>
    private void OnAddInteractVerb(Entity<NetworkConfiguratorComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.Using.HasValue)
            return;

        var verbArgs = args;
        var verb = new UtilityVerb
        {
            Act = () => OnUsed(ent, verbArgs.Target, verbArgs.User),
            Impact = LogImpact.Low
        };

        if (ent.Comp.LinkModeActive && (HasComp<DeviceLinkSinkComponent>(args.Target) || HasComp<DeviceLinkSourceComponent>(args.Target)))
        {
            var linkStarted = ent.Comp.ActiveDeviceLink.HasValue;
            verb.Text = Loc.GetString(linkStarted ? "network-configurator-link" : "network-configurator-start-link");
            verb.Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/in.svg.192dpi.png"));
            args.Verbs.Add(verb);
        }
        else if (HasComp<DeviceNetworkComponent>(args.Target))
        {
            var isDeviceList = HasComp<DeviceListComponent>(args.Target);
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

        var verbArgs = args;
        if (!configurator.LinkModeActive && HasComp<DeviceListComponent>(args.Target))
        {
            AlternativeVerb verb = new()
            {
                Text = Loc.GetString("network-configurator-save-device"),
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/in.svg.192dpi.png")),
                Act = () => TryAddNetworkDevice(verbArgs.Target, verbArgs.Using.Value, verbArgs.User),
                Impact = LogImpact.Low
            };
            args.Verbs.Add(verb);
            return;
        }

        if (configurator is { LinkModeActive: true, ActiveDeviceLink: not null }
            && (HasComp<DeviceLinkSinkComponent>(args.Target) || HasComp<DeviceLinkSourceComponent>(args.Target)))
        {
            AlternativeVerb verb = new()
            {
                Text = Loc.GetString("network-configurator-link-defaults"),
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/in.svg.192dpi.png")),
                Act = () => TryLinkDefaults(verbArgs.Using.Value, configurator, verbArgs.Target, verbArgs.User),
                Impact = LogImpact.Low,
            };
            args.Verbs.Add(verb);
        }
    }

    private void OnAddSwitchModeVerb(Entity<NetworkConfiguratorComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.Using.HasValue || !HasComp<NetworkConfiguratorComponent>(args.Target))
            return;

        var verbArgs = args;
        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("network-configurator-switch-mode"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
            Act = () => SwitchMode(verbArgs.User, ent),
            Impact = LogImpact.Low,
        };
        args.Verbs.Add(verb);
    }
}
