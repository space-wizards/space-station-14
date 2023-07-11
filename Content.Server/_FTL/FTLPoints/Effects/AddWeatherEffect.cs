using Content.Server.Weather;
using Content.Shared.Weather;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server._FTL.FTLPoints.Effects;

[DataDefinition]
public sealed class AddWeatherEffect : FTLPointEffect
{
    [DataField("weatherPrototypes", customTypeSerializer: typeof(PrototypeIdListSerializer<WeatherPrototype>))]
    public List<string> WeatherPrototypes { get; } = default!;

    public override void Effect(FTLPointEffectArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var protoManager = IoCManager.Resolve<IPrototypeManager>();
        var weatherSystem = args.EntityManager.System<WeatherSystem>();

        weatherSystem.SetWeather(args.MapId, protoManager.Index<WeatherPrototype>(random.Pick(WeatherPrototypes)), null);
        Logger.Debug("Set weather.");
    }
}
