using Content.Shared.Chemistry.Reagent;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Station;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.IoC;

namespace Content.Server.Chemistry.ReagentEffectConditions
{    
    public sealed partial class JobCondition : ReagentEffectCondition
    {
        //TO DO: make this use protoid
        [DataField] public List<string> Job = new List<string> {"Passenger"};
                
        public override bool Condition(ReagentEffectArgs args)
        {   
            args.EntityManager.TryGetComponent<MindContainerComponent>(args.SolutionEntity, out var mindContainer);
            if (mindContainer != null && mindContainer.Mind != null)
            {
                var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
                if (args.EntityManager.TryGetComponent<JobComponent>(mindContainer?.Mind, out var comp) && prototypeManager.TryIndex(comp.Prototype, out var prototype))
                {
                    foreach (var jobId in Job)
                    {
                        if (prototype.ID == jobId)
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }
        
        public override string GuidebookExplanation(IPrototypeManager prototype)
        {
            string Jobs = String.Join(" or ", Job.ToArray());
            return Loc.GetString("reagent-effect-condition-guidebook-job-condition", ("job", Jobs));
        }
    }
}

