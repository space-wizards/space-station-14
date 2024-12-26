using Content.Server.Anomaly.Components;
using Robust.Shared.Random;

namespace Content.Server.Anomaly.Effects;

public sealed class SecretDataAnomalySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly List<AnomalySecretData> _deita = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<SecretDataAnomalyComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, SecretDataAnomalyComponent anomaly, MapInitEvent args)
    {
        RandomizeSecret(uid,_random.Next(anomaly.RandomStartSecretMin, anomaly.RandomStartSecretMax), anomaly);
    }

    public void RandomizeSecret(EntityUid uid, int count, SecretDataAnomalyComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Secret.Clear();

        // I also considered just adding all the enum values and pruning but that seems more wasteful.
        _deita.Clear();
        _deita.AddRange(Enum.GetValues<AnomalySecretData>());
        var actualCount = Math.Min(count, _deita.Count);

        for (int i = 0; i < actualCount; i++)
        {
            component.Secret.Add(_random.PickAndTake(_deita));
        }
    }
}

