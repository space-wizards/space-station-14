using Content.Server.Heretic.EntitySystems;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Store;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed.Commands.Math;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.Store.Conditions;

public sealed partial class HereticPathCondition : ListingCondition
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly HereticKnowledgeSystem _knowledge = default!;

    public int AlternatePathPenalty = 1;
    [DataField] public HashSet<string>? Whitelist;
    [DataField] public HashSet<string>? Blacklist;
    [DataField] public int Stage = 0;
   
    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;
        var minds = ent.System<SharedMindSystem>();

        if (!minds.TryGetMind(args.Buyer, out var mindId, out var mind))
            return false;

        if (!ent.TryGetComponent<HereticComponent>(args.Buyer, out var hereticComp))
            return false;

        //Logger.Debug("Current Path: " + hereticComp.CurrentPath);
        Logger.Debug("Current Stage: " + hereticComp.PathStage);
        Logger.Debug("Working on Listing: " + args.Listing.Name );

        //Stage is the level of the knowledge we're looking at
        //always check for level
        if (Stage > hereticComp.PathStage)
        {
            return false;
        }
        //if you have chosen a path

        if (Whitelist != null)
        {
            Logger.Debug("doing Whitelist \n");
            foreach (var white in Whitelist)
                if (hereticComp.CurrentPath == white)
                    return true;
            return false;
        }

        if (Blacklist != null)
        {
            Logger.Debug("doing Blacklist \n");
            foreach (var black in Blacklist)
                if (hereticComp.CurrentPath == black)
                    return false;
            return true;
        }
        Logger.Debug("\n");



        if ((hereticComp.CurrentPath != null) && (args.Listing.ProductHereticKnowledge != null))
        {
            //and the knowledge you're looking at is not from your current path or side knowledge
            ProtoId<HereticKnowledgePrototype> knowledgeProto = new ProtoId<HereticKnowledgePrototype>((ProtoId<HereticKnowledgePrototype>)args.Listing.ProductHereticKnowledge);
            Logger.Debug("Listing: " + args.Listing.Name);
            Logger.Debug("ID: " + args.Listing.ID);
            Logger.Debug("knowledge: " + args.Listing.ProductHereticKnowledge);


            Logger.Debug("proto knowledge: " + knowledgeProto);
            Logger.Debug("proto ID: " + knowledgeProto.Id);

            var knowledge = _knowledge.GetKnowledge(knowledgeProto);
            Logger.Debug("knowledge path: " + knowledge.Path + "\n");
            Logger.Debug("user path: " + hereticComp.CurrentPath + "\n");
            Logger.Debug("knowledge Stage: " + knowledge.Stage + "\n");
            Logger.Debug("user Stage: " + hereticComp.PathStage + "\n");

            HashSet<string> myPaths = new HashSet<string>();
            myPaths.Add(hereticComp.CurrentPath);
            myPaths.Add("Side");


            if (knowledge.Path != null && !(myPaths.Contains(knowledge.Path)))
            {
                //then, there should be a penalty.
                //so, if the level of the knowledge is greater than your current path's level minus the penalty
                if (Stage > hereticComp.PathStage - AlternatePathPenalty)
                {
                    //then you can't have it.
                    return false;
                }

            }
        }
        
        return true;
    }
}
