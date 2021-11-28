using Content.Shared.Kudzu;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Random;

namespace Content.Server.Kudzu;

public class GrowingKudzuSystem : EntitySystem
{
    private float _accumulatedFrameTime = 0.0f;

    public override void Initialize()
    {
        SubscribeLocalEvent<GrowingKudzuComponent, ComponentAdd>(SetupKudzu);
    }

    private void SetupKudzu(EntityUid uid, GrowingKudzuComponent component, ComponentAdd args)
    {
        if (!EntityManager.TryGetComponent<AppearanceComponent>(uid, out var appearance))
        {
            Logger.Warning("Kudzu without an appearance component?");
            return;
        }

        appearance.SetData(KudzuVisuals.Variant, _robustRandom.Next(1, 3));
        appearance.SetData(KudzuVisuals.GrowthLevel, 1);
    }

    public override void Update(float frameTime)
    {
        _accumulatedFrameTime += frameTime;

        if (!(_accumulatedFrameTime >= 0.5f))
            return;

        _accumulatedFrameTime -= 0.5f;

        foreach (var (kudzu, appearance) in EntityManager.EntityQuery<GrowingKudzuComponent, AppearanceComponent>())
        {
            if (kudzu.GrowthLevel >= 3 || !_robustRandom.Prob(1.0f/6.0f)) continue;
            kudzu.GrowthLevel += 1;

            if (kudzu.GrowthLevel == 3 &&
                EntityManager.TryGetComponent<SpreaderComponent>(kudzu.OwnerUid, out var spreader))
            {
                // why cache when you can simply cease to be? Also saves a bit of memory/time.
                EntityManager.RemoveComponent<GrowingKudzuComponent>(kudzu.OwnerUid);
            }

            appearance.SetData(KudzuVisuals.GrowthLevel, kudzu.GrowthLevel);
        }
    }

    [Dependency] private readonly SpreaderSystem _spreaderSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
}
