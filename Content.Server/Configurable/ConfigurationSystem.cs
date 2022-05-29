using Content.Shared.Interaction;
using Content.Shared.Tools.Components;
using Robust.Server.GameObjects;
using static Content.Shared.Configurable.SharedConfigurationComponent;

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
    }
    
    private void OnInteractUsing(EntityUid uid, ConfigurationComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(args.Used, out ToolComponent? tool) || !tool.Qualities.Contains(component.QualityNeeded))
            return;

        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        args.Handled = _uiSystem.TryOpen(uid, ConfigurationUiKey.Key, actor.PlayerSession);
    }

    private void OnStartup(EntityUid uid, ConfigurationComponent component, ComponentStartup args)
    {
        UpdateUi(uid, component);
    }

    private void UpdateUi(EntityUid uid, ConfigurationComponent component)
    {
        if (_uiSystem.TryGetUi(uid, ConfigurationUiKey.Key, out var ui))
            ui.SetState(new ConfigurationBoundUserInterfaceState(component.Config));
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

        // TODO raise event.
        // TODO support float (spinbox) and enum (drop-down) configurations
        // TODO support verbs.
    }
}
