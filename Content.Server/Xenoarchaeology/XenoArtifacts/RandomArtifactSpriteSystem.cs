using System;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Xenoarchaeology.XenoArtifacts;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Xenoarchaeology.XenoArtifacts;

public class RandomArtifactSpriteSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RandomArtifactSpriteComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RandomArtifactSpriteComponent, ArtifactActivatedEvent>(OnActivated);
    }

    private void OnInit(EntityUid uid, RandomArtifactSpriteComponent component, ComponentInit args)
    {
        if (!EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            return;

        var randomSprite = _random.Next(component.MinSprite, component.MaxSprite + 1);
        appearance.SetData(SharedArtifactsVisuals.SpriteIndex, randomSprite);
    }

    private void OnActivated(EntityUid uid, RandomArtifactSpriteComponent component, ArtifactActivatedEvent args)
    {
        if (!EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            return;

        appearance.SetData(SharedArtifactsVisuals.IsActivated, true);

        var activationTime = TimeSpan.FromSeconds(component.ActivationTime);
        Timer.Spawn(activationTime, () =>
        {
            appearance.SetData(SharedArtifactsVisuals.IsActivated, false);
        });

    }
}
