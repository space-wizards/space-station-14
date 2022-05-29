using Content.Shared.CCVar;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class ExplosionSystem : EntitySystem
{
    public int MaxIterations { get; private set; }
    public int MaxArea { get; private set; }
    public float MaxProcessingTime { get; private set; }
    public int TilesPerTick { get; private set; }
    public int ThrowLimit { get; private set; }
    public bool SleepNodeSys { get; private set; }
    public bool IncrementalTileBreaking { get; private set; }
    public int SingleTickAreaLimit {get; private set; }

    private void SubscribeCvars()
    {
        _cfg.OnValueChanged(CCVars.ExplosionTilesPerTick, SetTilesPerTick, true);
        _cfg.OnValueChanged(CCVars.ExplosionThrowLimit, SetThrowLimit, true);
        _cfg.OnValueChanged(CCVars.ExplosionSleepNodeSys, SetSleepNodeSys, true);
        _cfg.OnValueChanged(CCVars.ExplosionMaxArea, SetMaxArea, true);
        _cfg.OnValueChanged(CCVars.ExplosionMaxIterations, SetMaxIterations, true);
        _cfg.OnValueChanged(CCVars.ExplosionMaxProcessingTime, SetMaxProcessingTime, true);
        _cfg.OnValueChanged(CCVars.ExplosionIncrementalTileBreaking, SetIncrementalTileBreaking, true);
        _cfg.OnValueChanged(CCVars.ExplosionSingleTickAreaLimit, SetSingleTickAreaLimit, true);
    }

    private void UnsubscribeCvars()
    {
        _cfg.UnsubValueChanged(CCVars.ExplosionTilesPerTick, SetTilesPerTick);
        _cfg.UnsubValueChanged(CCVars.ExplosionThrowLimit, SetThrowLimit);
        _cfg.UnsubValueChanged(CCVars.ExplosionSleepNodeSys, SetSleepNodeSys);
        _cfg.UnsubValueChanged(CCVars.ExplosionMaxArea, SetMaxArea);
        _cfg.UnsubValueChanged(CCVars.ExplosionMaxIterations, SetMaxIterations);
        _cfg.UnsubValueChanged(CCVars.ExplosionMaxProcessingTime, SetMaxProcessingTime);
        _cfg.UnsubValueChanged(CCVars.ExplosionIncrementalTileBreaking, SetIncrementalTileBreaking);
        _cfg.UnsubValueChanged(CCVars.ExplosionSingleTickAreaLimit, SetSingleTickAreaLimit);
    }

    private void SetTilesPerTick(int value) => TilesPerTick = value;
    private void SetThrowLimit(int value) => ThrowLimit = value;
    private void SetSleepNodeSys(bool value) => SleepNodeSys = value;
    private void SetMaxArea(int value) => MaxArea = value;
    private void SetMaxIterations(int value) => MaxIterations = value;
    private void SetMaxProcessingTime(float value) => MaxProcessingTime = value;
    private void SetIncrementalTileBreaking(bool value) => IncrementalTileBreaking = value;
    private void SetSingleTickAreaLimit(int value) => SingleTickAreaLimit = value;
}
