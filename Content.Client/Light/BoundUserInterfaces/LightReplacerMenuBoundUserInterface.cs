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

        if (hasActiveTubes && _prototype.Resolve(replacer.ActiveLightTube, out var ejectTube))
        {
            var option = AddEjectOption(replacer.ActiveLightTube, _ejectTubes, ejectTube.Name);
            options.Add(option);
        }

        if (hasActiveBulbs && _prototype.Resolve(replacer.ActiveLightBulb, out var ejectBulbs))
        {
            var option = AddEjectOption(replacer.ActiveLightBulb, _ejectBulbs, ejectBulbs.Name);
            options.Add(option);
        }

        // This iterates through every unique light to add them as options.
        foreach (var light in tubes)
        {
            var option = CreateOptions(light.Key, light.Value, LightBulbType.Tube);
            options.Add(option);
        }

        foreach (var light in bulbs)
        {
            var option = CreateOptions(light.Key, light.Value, LightBulbType.Bulb);
            options.Add(option);
        }

        return options;
    }

    private RadialMenuOptionBase AddEjectOption(EntProtoId protoToUseInAction, EntProtoId protoForIcon, string lightTypeName)
    {
        return new RadialMenuActionOption<EntProtoId>(EjectLights, protoToUseInAction)
        {
            IconSpecifier = RadialMenuIconSpecifier.With(protoForIcon),
            ToolTip = Loc.GetString("comp-light-replacer-eject-specified-lights", ("light", lightTypeName)),
        };
    }

    private RadialMenuOptionBase CreateOptions(EntProtoId protoId, string lightTypeName, LightBulbType lightType)
    {
        return new RadialMenuActionOption<(EntProtoId, LightBulbType)>(SwitchActiveLight, (protoId, lightType))
        {
            IconSpecifier = RadialMenuIconSpecifier.With(protoId),
            ToolTip = Loc.GetString("comp-light-replacer-select-lights", ("light", lightTypeName)),
        };
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
