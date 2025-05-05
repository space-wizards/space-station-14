using Content.Shared.Interaction;
using Content.Shared.Tools.Systems;
using Robust.Shared.Containers;
using static Content.Shared.Configurable.ConfigurationComponent;

namespace Content.Shared.Configurable;

/// <summary>
/// <see cref="ConfigurationComponent"/>
/// </summary>
public abstract class SharedConfigurationSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConfigurationComponent, ConfigurationUpdatedMessage>(OnUpdate);
        SubscribeLocalEvent<ConfigurationComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ConfigurationComponent, ContainerIsInsertingAttemptEvent>(OnInsert);
    }

    private void OnInteractUsing(EntityUid uid, ConfigurationComponent component, InteractUsingEvent args)
    {
        // TODO use activatable ui system
        if (args.Handled)
            return;

        if (!_toolSystem.HasQuality(args.Used, component.QualityNeeded))
            return;

        args.Handled = _uiSystem.TryOpenUi(uid, ConfigurationUiKey.Key, args.User);
    }

    private void OnUpdate(EntityUid uid, ConfigurationComponent component, ConfigurationUpdatedMessage args)
    {
        foreach (var key in component.Config.Keys)
        {
            var value = args.Config.GetValueOrDefault(key);

            if (string.IsNullOrWhiteSpace(value) || component.Validation != null && !component.Validation.IsMatch(value))
                continue;

            component.Config[key] = value;
        }

        Dirty(uid, component);
        var updatedEvent = new ConfigurationUpdatedEvent(component);
        RaiseLocalEvent(uid, updatedEvent);

        // TODO support float (spinbox) and enum (drop-down) configurations
        // TODO support verbs.
    }

    private void OnInsert(EntityUid uid, ConfigurationComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if (!_toolSystem.HasQuality(args.EntityUid, component.QualityNeeded))
            return;

        args.Cancel();
    }
}

/// <summary>
/// Sent when configuration values got changes
/// </summary>
public sealed class ConfigurationUpdatedEvent : EntityEventArgs
{
    public ConfigurationComponent Configuration;

    public ConfigurationUpdatedEvent(ConfigurationComponent configuration)
    {
        Configuration = configuration;
    }
}
