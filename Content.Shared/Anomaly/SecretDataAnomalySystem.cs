using Content.Shared.Anomaly.Effects.Components;
using Robust.Shared.Random;

namespace Content.Shared.Anomaly.Effects;

public sealed class SecretDataAnomalySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<SecretDataAnomalyComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, SecretDataAnomalyComponent anomaly, ComponentInit args)
    {
        RandomizeSecret(uid,_random.Next(anomaly.RandomStartSecretMin, anomaly.RandomStartSecretMax), anomaly);
    }

    public void RandomizeSecret(EntityUid uid, int count, SecretDataAnomalyComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Secret.Clear();

        var variants = new List<AnomalySecretData> {
            AnomalySecretData.Severity,
            AnomalySecretData.Stability,
            AnomalySecretData.OutputPoint,
            AnomalySecretData.ParticleDanger,
            AnomalySecretData.ParticleUnstable,
            AnomalySecretData.ParticleContainment,
            AnomalySecretData.ParticleTransformation,
            AnomalySecretData.Behaviour,
        };

        for (int i = 0; i < count; i++)
        {
            component.Secret.Add(_random.PickAndTake(variants));
        }
    }
}

