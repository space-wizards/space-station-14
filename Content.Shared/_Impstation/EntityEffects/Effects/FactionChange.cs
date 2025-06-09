using Content.Shared.EntityEffects;
using Content.Shared.Mind.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.EntityEffects.Effects;

///Ty to Mqole, Ada and Ruddy for the help. 

public sealed partial class FactionChange : EntityEffect
{
    [DataField]
    public ProtoId<NpcFactionPrototype> Faction;
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    => Loc.GetString("reagent-effect-guidebook-factionchange", ("chance", Probability));

    public override void Effect(EntityEffectBaseArgs args)
    {

        //get these out of the args to be less annoying
        var entMan = args.EntityManager;
        var ent = args.TargetEntity;

        //stops it from applying to player-controlled entities
        if (entMan.TryGetComponent<MindContainerComponent>(ent, out var mindContainer) && mindContainer.HasMind)
        {
            return;
        }

        //do nothing if the faction has no faction member comp
        if (!entMan.TryGetComponent<NpcFactionMemberComponent>(ent, out var npcFactionMember))
            return;

        //make it a tuple so we don't have to re-tuple it twice for the factionSystem calls
        var entAsTuple = (ent, npcFactionMember);

        //get the faction system from the ent manager
        var factionSystem = entMan.System<NpcFactionSystem>();

        ///do the factionSystem stuff
        factionSystem.ClearFactions(entAsTuple);
        factionSystem.AddFaction(entAsTuple, Faction);
    }

}