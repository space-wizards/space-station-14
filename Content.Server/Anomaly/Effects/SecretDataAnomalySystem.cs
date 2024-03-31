using Content.Server.Anomaly.Components;
using Robust.Shared.Random;

namespace Content.Server.Anomaly.Effects;

public sealed class SecretDataAnomalySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
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

        var variants = Enum.GetValues<AnomalySecretData>();

        for (int i = 0; i < count; i++)
        {
            component.Secret.Add(_random.PickAndTake(variants));
        }
    }
}

