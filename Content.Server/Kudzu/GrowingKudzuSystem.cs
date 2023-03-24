using Content.Shared.Kudzu;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server.Kudzu;

public sealed class GrowingKudzuSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private float _accumulatedFrameTime = 0.0f;

    public override void Initialize()
    {
        SubscribeLocalEvent<GrowingKudzuComponent, ComponentAdd>(SetupKudzu);
    }

    private void SetupKudzu(EntityUid uid, GrowingKudzuComponent component, ComponentAdd args)
    {
        if (!EntityManager.TryGetComponent<AppearanceComponent>(uid, out var appearance))
        {
            return;
        }

        _appearance.SetData(uid, KudzuVisuals.Variant, _robustRandom.Next(1, 3), appearance);
        _appearance.SetData(uid, KudzuVisuals.GrowthLevel, 1, appearance);
    }

    public override void Update(float frameTime)
    {
        _accumulatedFrameTime += frameTime;

        if (!(_accumulatedFrameTime >= 0.5f))
            return;

        _accumulatedFrameTime -= 0.5f;

        foreach (var (kudzu, appearance) in EntityManager.EntityQuery<GrowingKudzuComponent, AppearanceComponent>())
        {
            if (kudzu.GrowthLevel >= 3 || !_robustRandom.Prob(kudzu.GrowthTickSkipChange)) continue;
            kudzu.GrowthLevel += 1;

            if (kudzu.GrowthLevel == 3 &&
                EntityManager.TryGetComponent<SpreaderComponent>((kudzu).Owner, out var spreader))
            {
                // why cache when you can simply cease to be? Also saves a bit of memory/time.
                EntityManager.RemoveComponent<GrowingKudzuComponent>((kudzu).Owner);
            }

            _appearance.SetData(kudzu.Owner, KudzuVisuals.GrowthLevel, kudzu.GrowthLevel, appearance);
        }
    }
}
