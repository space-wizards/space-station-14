using Content.Client.UserInterface.Controls;
using Content.Shared.Light.Components;
using Content.Shared.Light.Events;
using Content.Shared.Storage.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Light.BoundUserInterfaces;

[UsedImplicitly]
public sealed partial class LightReplacerMenuBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private IPrototypeManager _prototype = default!;

    private SimpleRadialMenu? _menu;

    private readonly EntProtoId _ejectTubes = "EjectTubes";
    private readonly EntProtoId _ejectBulbs = "EjectBulbs";

    protected override void Open()
    {
        base.Open();
        IoCManager.InjectDependencies(this);

        if (!EntMan.TryGetComponent<LightReplacerComponent>(Owner, out var replacer)
            || !EntMan.TryGetComponent<EntityProviderComponent>(Owner, out var provider))
            return;

        var lightTypes = CreateButtons(replacer, provider);

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.SetButtons(lightTypes);

        _menu.OpenCentered();
    }

    private List<RadialMenuOptionBase> CreateButtons(LightReplacerComponent replacer, EntityProviderComponent provider)
    {
        var options = new List<RadialMenuOptionBase>();

        Dictionary<EntProtoId, string> tubes = [];
        Dictionary<EntProtoId, string> bulbs = [];

        var hasActiveTubes = false;
        var hasActiveBulbs = false;

        foreach (var lightProtoId in provider.EntityCounter)
        {
            if (!_prototype.Resolve(lightProtoId.Key, out var light)
                || !light.Components.TryGetComponent<LightBulbComponent>(EntMan.ComponentFactory, out var bulb))
                continue;

            if (bulb.Type == LightBulbType.Tube)
            {
                if (lightProtoId.Key != replacer.ActiveLightTube)
                    tubes.TryAdd(lightProtoId.Key, light.Name);
                else
                    hasActiveTubes = true;
            }
            else
            {
                if (lightProtoId.Key != replacer.ActiveLightBulb)
                    bulbs.TryAdd(lightProtoId.Key, light.Name);
                else
                    hasActiveBulbs = true;
            }
        }

        if (hasActiveTubes && _prototype.Resolve(replacer.ActiveLightTube, out var ejectTubes))
        {
            var toggleLightTubes = new RadialMenuActionOption<EntProtoId>(EjectLights, replacer.ActiveLightTube)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(_ejectTubes),
                ToolTip = Loc.GetString("comp-light-replacer-eject-specified-lights", ("light", ejectTubes.Name)),
            };
            options.Add(toggleLightTubes);
        }

        if (hasActiveBulbs && _prototype.Resolve(replacer.ActiveLightBulb, out var ejectBulbs))
        {
            var toggleLightBulbs = new RadialMenuActionOption<EntProtoId>(EjectLights, replacer.ActiveLightBulb)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(_ejectBulbs),
                ToolTip = Loc.GetString("comp-light-replacer-eject-specified-lights", ("light", ejectBulbs.Name)),
            };
            options.Add(toggleLightBulbs);
        }

        // This iterates through every unique light to add them as options.
        foreach (var light in tubes)
        {
            PopulateOptions(light.Key, light.Value, LightBulbType.Tube, ref options);
        }

        foreach (var light in bulbs)
        {
            PopulateOptions(light.Key, light.Value, LightBulbType.Bulb, ref options);
        }

        return options;
    }

    private void PopulateOptions(EntProtoId protoId, string name, LightBulbType lightType, ref List<RadialMenuOptionBase> options)
    {
        var switchLight = new RadialMenuActionOption<(EntProtoId, LightBulbType)>(SwitchActiveLight, (protoId, lightType))
        {
            IconSpecifier = RadialMenuIconSpecifier.With(protoId),
            ToolTip = Loc.GetString("comp-light-replacer-select-lights", ("light", name)),
        };
        options.Add(switchLight);
    }

    private void SwitchActiveLight((EntProtoId, LightBulbType) light)
    {
        var message = new SwitchLightTypeMessage(light);
        SendPredictedMessage(message);
    }

    private void EjectLights(EntProtoId lightName)
    {
        var message = new EjectLightTypeMessage(lightName);
        SendPredictedMessage(message);
    }
}
