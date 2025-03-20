using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Item;
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
    [Dependency] private readonly SharedItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RandomArtifactSpriteComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RandomArtifactSpriteComponent, ArtifactActivatedEvent>(OnActivated);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RandomArtifactSpriteComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out var component, out var appearance))
        {
            if (component.ActivationStart == null)
                continue;

            var timeDif = _time.CurTime - component.ActivationStart.Value;
            if (timeDif.Seconds >= component.ActivationTime)
            {
                _appearance.SetData(uid, SharedArtifactsVisuals.IsActivated, false, appearance);
                component.ActivationStart = null;
            }
        }
    }

    private void OnMapInit(EntityUid uid, RandomArtifactSpriteComponent component, MapInitEvent args)
    {
        var artiType = EntityManager.GetComponent<ArtifactComponent>(uid).ArtiType;
        var randomSpriteChoices = Array.Empty<int>();
        switch (artiType)
        {
            case ArtiOrigin.Eldritch:
                randomSpriteChoices = component.EldritchSprites;
                break;
            case ArtiOrigin.Martian:
                randomSpriteChoices = component.MartianSprites;
                break;
            case ArtiOrigin.Precursor:
                randomSpriteChoices = component.PrecursorSprites;
                break;
            case ArtiOrigin.Silicon:
                randomSpriteChoices = component.SiliconSprites;
                break;
            case ArtiOrigin.Wizard:
                randomSpriteChoices = component.WizardSprites;
                break;
        }
        var randomSprite = _random.Next(0, randomSpriteChoices.Length);
        _appearance.SetData(uid, SharedArtifactsVisuals.SpriteIndex, randomSpriteChoices[randomSprite]);
        _item.SetHeldPrefix(uid, "ano" + randomSprite.ToString("D2")); //set item artifact inhands
    }

    private void OnActivated(EntityUid uid, RandomArtifactSpriteComponent component, ArtifactActivatedEvent args)
    {
        _appearance.SetData(uid, SharedArtifactsVisuals.IsActivated, true);
        component.ActivationStart = _time.CurTime;
    }
}
