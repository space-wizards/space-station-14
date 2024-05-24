using Content.Shared.Configurable;
using Content.Shared.Interaction;
using Content.Shared.Tools.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using static Content.Shared.Configurable.ConfigurationComponent;

namespace Content.Server.Configurable;

public sealed class ConfigurationSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConfigurationComponent, ConfigurationUpdatedMessage>(OnUpdate);
        SubscribeLocalEvent<ConfigurationComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ConfigurationComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ConfigurationComponent, ContainerIsInsertingAttemptEvent>(OnInsert);
    }

    private void OnInteractUsing(EntityUid uid, ConfigurationComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(args.Used, out ToolComponent? tool) || !tool.Qualities.Contains(component.QualityNeeded))
            return;

        args.Handled = _uiSystem.TryOpenUi(uid, ConfigurationUiKey.Key, args.User);
    }

    private void OnStartup(EntityUid uid, ConfigurationComponent component, ComponentStartup args)
    {
        UpdateUi(uid, component);
    }

    private void UpdateUi(EntityUid uid, ConfigurationComponent component)
    {
        if (_uiSystem.HasUi(uid, ConfigurationUiKey.Key))
            _uiSystem.SetUiState(uid, ConfigurationUiKey.Key, new ConfigurationBoundUserInterfaceState(component.Config));
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

        UpdateUi(uid, component);

        var updatedEvent = new ConfigurationUpdatedEvent(component);
        RaiseLocalEvent(uid, updatedEvent, false);

        // TODO support float (spinbox) and enum (drop-down) configurations
        // TODO support verbs.
    }

    private void OnInsert(EntityUid uid, ConfigurationComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if (!TryComp(args.EntityUid, out ToolComponent? tool) || !tool.Qualities.Contains(component.QualityNeeded))
            return;

        args.Cancel();
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
}
