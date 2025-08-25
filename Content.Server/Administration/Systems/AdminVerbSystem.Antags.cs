using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Zombies;
using Content.Server.Clothing.Systems;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly ZombieSystem _zombie = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly OutfitSystem _outfit = default!;

    private static readonly EntProtoId DefaultTraitorRule = "Traitor";
    private static readonly EntProtoId DefaultInitialInfectedRule = "Zombie";
    private static readonly EntProtoId DefaultNukeOpRule = "LoneOpsSpawn";
    private static readonly EntProtoId DefaultRevsRule = "Revolutionary";
    private static readonly EntProtoId DefaultThiefRule = "Thief";
    private static readonly EntProtoId DefaultChangelingRule = "Changeling";
    private static readonly EntProtoId ParadoxCloneRuleId = "ParadoxCloneSpawn";
    private static readonly ProtoId<StartingGearPrototype> PirateGearId = "PirateGear";

    protected override void AntagForceTraitorVerb(ICommonSession target)
    {
        _antag.ForceMakeAntag<TraitorRuleComponent>(target, DefaultTraitorRule);
    }

    protected override void AntagForceInitialInfectedVerb(ICommonSession target)
    {
        _antag.ForceMakeAntag<ZombieRuleComponent>(target, DefaultInitialInfectedRule);
    }

    protected override void AntagForceZombifyVerb(EntityUid target)
    {
        _zombie.ZombifyEntity(target);
    }

    protected override void AntagForceNukeOpsVerb(ICommonSession target)
    {
        _antag.ForceMakeAntag<NukeopsRuleComponent>(target, DefaultNukeOpRule);
    }

    protected override void AntagForcePirateVerb(EntityUid target)
    {
        // pirates just get an outfit because they don't really have logic associated with them
        _outfit.SetOutfit(target, PirateGearId);
    }

    protected override void AntagForceRevVerb(ICommonSession target)
    {
        _antag.ForceMakeAntag<RevolutionaryRuleComponent>(target, DefaultRevsRule);
    }

    protected override void AntagForceThiefVerb(ICommonSession target)
    {
        _antag.ForceMakeAntag<ThiefRuleComponent>(target, DefaultThiefRule);
    }

    protected override void AntagForceChanglingVerb(ICommonSession target)
    {
        _antag.ForceMakeAntag<ChangelingRuleComponent>(target, DefaultChangelingRule);
    }

    protected override void AntagForceParadoxCloneVerb(EntityUid target)
    {
        var ruleEnt = _gameTicker.AddGameRule(ParadoxCloneRuleId);

        if (!TryComp<ParadoxCloneRuleComponent>(ruleEnt, out var paradoxCloneRuleComp))
            return;

        paradoxCloneRuleComp.OriginalBody = target; // override the target player

        _gameTicker.StartGameRule(ruleEnt);
    }
}
