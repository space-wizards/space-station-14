using Content.Shared.CCVar;

namespace Content.Shared.GameTicking;

public abstract partial class GameTicker
{
    protected virtual void InitializeCVars()
    {
        Subs.CVar(Cfg, CCVars.GameTickerIgnoredPresets, value => _ignoredRules = value.Split(","));
    }
}
