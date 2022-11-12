using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Xenoarchaeology.XenoArtifacts;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Xenoarchaeology.XenoArtifacts;

public sealed class RandomArtifactSpriteSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

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
                _appearance.SetData(appearance.Owner, SharedArtifactsVisuals.IsActivated, false, appearance);
                component.ActivationStart = null;
            }
        }
    }

    private void OnMapInit(EntityUid uid, RandomArtifactSpriteComponent component, MapInitEvent args)
    {
        var randomSprite = _random.Next(component.MinSprite, component.MaxSprite + 1);
        _appearance.SetData(uid, SharedArtifactsVisuals.SpriteIndex, randomSprite);
    }

    private void OnActivated(EntityUid uid, RandomArtifactSpriteComponent component, ArtifactActivatedEvent args)
    {
        _appearance.SetData(uid, SharedArtifactsVisuals.IsActivated, true);
        component.ActivationStart = _time.CurTime;
    }
}
