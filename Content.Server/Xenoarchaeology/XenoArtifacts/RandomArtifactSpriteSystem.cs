using System;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Xenoarchaeology.XenoArtifacts;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Xenoarchaeology.XenoArtifacts;

public sealed class RandomArtifactSpriteSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _time = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RandomArtifactSpriteComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RandomArtifactSpriteComponent, ArtifactActivatedEvent>(OnActivated);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityManager.EntityQuery<RandomArtifactSpriteComponent, AppearanceComponent>();
        foreach (var (component, appearance) in query)
        {
            if (component.ActivationStart == null)
                continue;

            var timeDif = _time.CurTime - component.ActivationStart.Value;
            if (timeDif.Seconds >= component.ActivationTime)
            {
                appearance.SetData(SharedArtifactsVisuals.IsActivated, false);
                component.ActivationStart = null;
            }
        }
    }

    private void OnMapInit(EntityUid uid, RandomArtifactSpriteComponent component, MapInitEvent args)
    {
        if (!TryComp(uid, out AppearanceComponent? appearance))
            return;

        var randomSprite = _random.Next(component.MinSprite, component.MaxSprite + 1);
        appearance.SetData(SharedArtifactsVisuals.SpriteIndex, randomSprite);
    }

    private void OnActivated(EntityUid uid, RandomArtifactSpriteComponent component, ArtifactActivatedEvent args)
    {
        if (!TryComp(uid, out AppearanceComponent? appearance))
            return;

        appearance.SetData(SharedArtifactsVisuals.IsActivated, true);
        component.ActivationStart = _time.CurTime;
    }
}
