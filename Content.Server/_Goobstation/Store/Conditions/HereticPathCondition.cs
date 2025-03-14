using Content.Server.Heretic.EntitySystems;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Server.Store.Conditions;

public sealed partial class HereticPathCondition : ListingCondition
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public int AlternatePathPenalty = 1; //you can only buy alternate paths' abilities if they are this amount under your initial path's top ability level.
    [DataField] public HashSet<string>? Whitelist;
    [DataField] public HashSet<string>? Blacklist;
    [DataField] public int Stage = 0;

    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;
        var minds = ent.System<SharedMindSystem>();
        var knowledgeSys = ent.System<HereticKnowledgeSystem>();



        if (!ent.TryGetComponent<MindComponent>(args.Buyer, out var mind))
            return false;

        if (!ent.TryGetComponent<HereticComponent>(mind.OwnedEntity, out var hereticComp))
            return false;

        //Stage is the level of the knowledge we're looking at
        //always check for level
        if (Stage > hereticComp.PathStage)
        {
            return false;
        }

        if (Whitelist != null)
        {
            foreach (var white in Whitelist)
                if (hereticComp.CurrentPath == white)
                    return true;
            return false;
        }

        if (Blacklist != null)
        {
            foreach (var black in Blacklist)
                if (hereticComp.CurrentPath == black)
                    return false;
            return true;
        }


        //if you have chosen a path
        if ((hereticComp.CurrentPath != null) && (args.Listing.ProductHereticKnowledge != null))
        {
            ProtoId<HereticKnowledgePrototype> knowledgeProtoId = new ProtoId<HereticKnowledgePrototype>((ProtoId<HereticKnowledgePrototype>)args.Listing.ProductHereticKnowledge);
            var knowledge = knowledgeSys.GetKnowledge(knowledgeProtoId);
            HashSet<string> myPaths = new HashSet<string>();
            myPaths.Add(hereticComp.CurrentPath);
            myPaths.Add("Side");

            //and the knowledge you're looking at is not from your current path or side knowledge
            if (knowledge.Path != null && !(myPaths.Contains(knowledge.Path)))
            {
                //then, there should be a penalty.
                //so, if the level of the knowledge is greater than your current path's level minus the penalty
                if (Stage > hereticComp.PathStage - AlternatePathPenalty)
                {
                    //then you can't have it.
                    return false;
                    //this took me two days.
                }

            }
        }
        return true;
    }
}
