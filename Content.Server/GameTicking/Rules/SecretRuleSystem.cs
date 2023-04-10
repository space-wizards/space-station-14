using System.Linq;
using Content.Server.GameTicking.Presets;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

public sealed class SecretRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _ticker = default!;

    public override string Prototype => "Secret";

    public override void Started()
    {
        PickRule();
    }

    public override void Ended()
    {
        // Preset should already handle it.
    }

    private void PickRule()
    {
        // TODO: This doesn't consider what can't start due to minimum player count, but currently there's no way to know anyway.
        // as they use cvars.
        var preset = _prototypeManager.Index<WeightedRandomPrototype>("Secret").Pick(_random);
        Logger.InfoS("gamepreset", $"Selected {preset} for secret.");

        foreach (var rule in _prototypeManager.Index<GamePresetPrototype>(preset).Rules)
        {
            _ticker.StartGameRule(_prototypeManager.Index<GameRulePrototype>(rule));
        }
    }
}
