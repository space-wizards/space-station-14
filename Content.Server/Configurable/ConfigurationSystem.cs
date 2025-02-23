using Content.Shared.Configurable;
using Content.Shared.Interaction;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Utility;
using static Content.Shared.Configurable.ConfigurationComponent;

namespace Content.Server.Configurable;

public sealed class ConfigurationSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConfigurationComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
        SubscribeLocalEvent<ConfigurationComponent, ConfigurationUpdatedMessage>(OnUpdate);
        SubscribeLocalEvent<ConfigurationComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ConfigurationComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ConfigurationComponent, ContainerIsInsertingAttemptEvent>(OnInsert);
    }

    private void OnGetInteractionVerbs(Entity<ConfigurationComponent> entity, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (args.Using is not { } tool || !_toolSystem.HasQuality(tool, entity.Comp.QualityNeeded))
            return;

        // Manually capture this value because C# doesn't want to implicitly capture in the lambda below from a `ref` value.
        var user = args.User;

        args.Verbs.Add(new InteractionVerb
        {
            Act = () => { _uiSystem.TryOpenUi(entity.Owner, ConfigurationUiKey.Key, user); },
            Text = Loc.GetString("configure-verb-get-data-text"),
            Message = Loc.GetString("anomaly-sync-connect-verb-message", ("configurable", entity)),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
        });
    }

    private void OnInteractUsing(Entity<ConfigurationComponent> entity, ref InteractUsingEvent args)
    {
        // TODO use activatable ui system
        if (args.Handled || !_toolSystem.HasQuality(args.Used, entity.Comp.QualityNeeded))
            return;

        args.Handled = _uiSystem.TryOpenUi(entity.Owner, ConfigurationUiKey.Key, args.User);
    }

    private void OnStartup(Entity<ConfigurationComponent> entity, ref ComponentStartup args)
    {
        UpdateUi(entity);
    }

    private void UpdateUi(Entity<ConfigurationComponent> entity)
    {
        if (_uiSystem.HasUi(entity, ConfigurationUiKey.Key))
        {
            _uiSystem.SetUiState(entity.Owner,
                ConfigurationUiKey.Key,
                new ConfigurationBoundUserInterfaceState(entity.Comp.Config));
        }
    }

    private void OnUpdate(Entity<ConfigurationComponent> entity, ref ConfigurationUpdatedMessage args)
    {
        foreach (var key in entity.Comp.Config.Keys)
        {
            var value = args.Config.GetValueOrDefault(key);

            if (string.IsNullOrWhiteSpace(value) ||
                entity.Comp.Validation != null && !entity.Comp.Validation.IsMatch(value))
                continue;

            entity.Comp.Config[key] = value;
        }

        UpdateUi(entity);

        var updatedEvent = new ConfigurationUpdatedEvent(entity);
        RaiseLocalEvent(entity, updatedEvent);

        // TODO support float (spinbox) and enum (drop-down) configurations
        // TODO support verbs.
    }

    private void OnInsert(Entity<ConfigurationComponent> entity, ref ContainerIsInsertingAttemptEvent args)
    {
        if (!_toolSystem.HasQuality(args.EntityUid, entity.Comp.QualityNeeded))
            return;

        args.Cancel();
    }

    /// <summary>
    /// Sent when configuration values got changes
    /// </summary>
    public sealed class ConfigurationUpdatedEvent(ConfigurationComponent configuration) : EntityEventArgs
    {
        public ConfigurationComponent Configuration = configuration;
    }
}
