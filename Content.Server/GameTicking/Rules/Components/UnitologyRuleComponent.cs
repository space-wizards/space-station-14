// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Audio;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Component for the UnitologyRuleSystem that stores info about winning/losing, player counts required for starting, as well as prototypes for unitologs and their gear.
/// </summary>
[RegisterComponent, Access(typeof(UnitologyRuleSystem))]
public sealed partial class UnitologyRuleComponent : Component
{
    /// <summary>
    /// Sound that plays when you are chosen as unitologs.
    /// </summary>
    [DataField]
    public SoundSpecifier UniStartSound = new SoundPathSpecifier("/Audio/_DeadSpace/Necromorfs/unitolog_start.ogg");

    /// <summary>
    /// Sound that plays before convergence stage.
    /// </summary>
    [DataField]
    public SoundSpecifier ConvergenceMusic = new SoundCollectionSpecifier("ConvergenceMusic");

    /// <summary>
    ///     Check if convergence music starts playing so we don't do it again
    /// </summary>
    public bool PlayedConvergenceSong = false;

    [DataField]
    public bool IsEndConvergence = false;

    [DataField]
    public bool IsStageObelisk = false;

    [DataField]
    public bool IsStageConvergence = false;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public EntityUid Obelisk;


    [DataField]
    public TimeSpan NextStageTime;

    [DataField("stageObeliskDuration")]
    public TimeSpan StageObeliskDuration = TimeSpan.FromMinutes(30);

    [DataField("stageConvergenceDuration")]
    public TimeSpan StageConvergenceDuration = TimeSpan.FromMinutes(1);
}

[ByRefEvent]
public readonly record struct StageConvergenceEvent();

[ByRefEvent]
public readonly record struct EndStageConvergenceEvent();

[ByRefEvent]
public readonly record struct StageObeliskEvent(EntityUid Obelisk);
