using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(HereticRuleSystem))]
public sealed partial class HereticRuleComponent : Component
{
    public readonly List<EntityUid> Minds = new();

    public readonly List<ProtoId<StoreCategoryPrototype>> StoreCategories = new()
    {
        "HereticPathAsh",
        //"HereticPathLock", //TODO
        "HereticPathFlesh",
        //"HereticPathBlade", //TODO
        "HereticPathVoid",
        //"HereticPathRust", //TODO
        "HereticPathSide"
    };

    /// <summary>
    ///     The time at which the upcoming wave of tomes will show up.
    ///     the (5) here defines the first time.
    /// </summary>
    public TimeSpan TimeOfNextWave = TimeSpan.FromMinutes(5);//heretics roll at about 1 minute in. hopefully 4 more minutes is enough to let newbies get situated

    /// <summary>
    ///     Waves will happen randomly at an interval of TimeBetweenWaves minutes +- this many seconds
    /// </summary>
    public readonly int RandomSecondsBuffer = 60;

    /// <summary>
    ///     The interval between waves of tomes.
    ///     This is the base time - the actual times are slightly randomized.
    /// </summary>
    public readonly TimeSpan TimeBetweenWaves = TimeSpan.FromMinutes(10); //about 10 waves total, assuming a full-length shift

    /// <summary>
    ///     whether the initial wave of tomes has gone out.
    /// </summary>
    public bool InitialWaveComplete = false;
}
