using Content.Client.UserInterface.Controls;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Storage.Events;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Storage.BoundUserInterfaces;

[UsedImplicitly]
public sealed partial class EntityProviderBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private IPrototypeManager _prototype = default!;
    private EntityProviderSystem _provider = default!;

    private SimpleRadialMenu? _menu;

    private readonly SpriteSpecifier.Texture _ejectSprite = new(new ResPath("Interface/VerbIcons/eject.svg.192dpi.png"));

    protected override void Open()
    {
        base.Open();
        IoCManager.InjectDependencies(this);
        _provider = EntMan.System<EntityProviderSystem>();

        if (!EntMan.TryGetComponent<EntityProviderComponent>(Owner, out var provider))
            return;

        var selectableEntities = CreateButtons(provider);

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.SetButtons(selectableEntities);

        _menu.OpenCentered();
    }

    private List<RadialMenuOptionBase> CreateButtons(EntityProviderComponent provider)
    {
        var options = new List<RadialMenuOptionBase>();

        if (!_provider.TryGetEntityCounter((Owner, provider), out var entityCounter))
            return options;

        // If an entity prototype ID is currently selected AND present, add an "eject all" option.
        if (provider.SelectedEntityProtoId != null
            && entityCounter.ContainsKey(provider.SelectedEntityProtoId.Value)
            && _prototype.Resolve(provider.SelectedEntityProtoId, out var selectedPrototype))
        {
            var option = new RadialMenuActionOption<EntProtoId>(EjectActiveEntities, provider.SelectedEntityProtoId.Value)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(_ejectSprite),
                ToolTip = Loc.GetString("comp-entity-provider-eject-all-specified-entities", ("entity", selectedPrototype.Name)),
            };
            options.Add(option);
        }

        foreach (var entityProtoId in entityCounter)
        {
            if (entityProtoId.Key == provider.SelectedEntityProtoId
                || !_prototype.Resolve(entityProtoId.Key, out var entityPrototype))
                continue;

            var option = new RadialMenuActionOption<EntProtoId>(SwitchActiveEntity, entityPrototype)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(entityPrototype),
                ToolTip = Loc.GetString("comp-entity-provider-select-entity", ("entity", entityPrototype.Name)),
            };
            options.Add(option);
        }

        return options;
    }

    private void SwitchActiveEntity(EntProtoId entityProtoId)
    {
        var message = new SwitchSelectedEntity(entityProtoId);
        SendPredictedMessage(message);
    }

    private void EjectActiveEntities(EntProtoId entityProtoId)
    {
        var message = new EjectSelectedEntities(entityProtoId);
        SendPredictedMessage(message);
    }
}
