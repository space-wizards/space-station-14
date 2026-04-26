using Content.Client.UserInterface.Controls;
using Content.Shared.Light.Components;
using Content.Shared.Light.Events;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Light.BoundUserInterfaces;

[UsedImplicitly]
public sealed class LightReplacerMenuBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private EntityQuery<LightBulbComponent> _lightBulbQuery;
    private EntityQuery<MetaDataComponent> _metaDataQuery;

    private SimpleRadialMenu? _menu;

    private readonly EntProtoId _ejectTubes = "EjectTubes";
    private readonly EntProtoId _ejectBulbs = "EjectBulbs";

    protected override void Open()
    {
        base.Open();

        _lightBulbQuery = EntMan.GetEntityQuery<LightBulbComponent>();
        _metaDataQuery = EntMan.GetEntityQuery<MetaDataComponent>();

        if (!EntMan.TryGetComponent<LightReplacerComponent>(Owner, out var replacer))
            return;

        var lightTypes = CreateButtons(replacer);

        if (lightTypes == null)
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.SetButtons(lightTypes);

        _menu.OpenCentered();
    }

    private IEnumerable<RadialMenuOptionBase>? CreateButtons(LightReplacerComponent replacer)
    {
        var options = new List<RadialMenuOptionBase>();

        Dictionary<string, EntityUid> tubes = [];
        Dictionary<string, EntityUid> bulbs = [];

        var hasActiveTubes = false;
        var hasActiveBulbs = false;

        foreach (var light in replacer.InsertedBulbs.ContainedEntities)
        {
            if (!_lightBulbQuery.TryComp(light, out var bulb)
                || !_metaDataQuery.TryComp(light, out var metaData))
                continue;

            if (bulb.Type == LightBulbType.Tube)
            {
                if (metaData.EntityName != replacer.ActiveLightTube)
                    tubes.TryAdd(metaData.EntityName, light);
                else
                    hasActiveTubes = true;
            }
            else
            {
                if (metaData.EntityName != replacer.ActiveLightBulb)
                    bulbs.TryAdd(metaData.EntityName, light);
                else
                    hasActiveBulbs = true;
            }
        }

        if (hasActiveTubes)
        {
            var toggleLightTubes = new RadialMenuActionOption<string>(EjectLights, replacer.ActiveLightTube)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(_ejectTubes),
                ToolTip = Loc.GetString("comp-light-replacer-eject-specified-lights", ("light", replacer.ActiveLightTube)),
            };
            options.Add(toggleLightTubes);
        }

        if (hasActiveBulbs)
        {
            var toggleLightBulbs = new RadialMenuActionOption<string>(EjectLights, replacer.ActiveLightBulb)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(_ejectBulbs),
                ToolTip = Loc.GetString("comp-light-replacer-eject-specified-lights", ("light", replacer.ActiveLightBulb)),
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

    private void PopulateOptions(string name, EntityUid uid, LightBulbType lightType, ref List<RadialMenuOptionBase> options)
    {
        var switchLight = new RadialMenuActionOption<(string, LightBulbType)>(SwitchActiveLight, (name, lightType))
        {
            IconSpecifier = RadialMenuIconSpecifier.With(uid),
            ToolTip = Loc.GetString("comp-light-replacer-select-lights", ("light", uid)),
        };
        options.Add(switchLight);
    }

    private void SwitchActiveLight((string, LightBulbType) light)
    {
        var message = new SwitchLightTypeMessage(light);
        SendPredictedMessage(message);
    }

    private void EjectLights(string lightName)
    {
        var message = new EjectLightTypeMessage(lightName);
        SendPredictedMessage(message);
    }
}
