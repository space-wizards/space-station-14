using Content.Server.Forensics;
using Content.Server.Chat.Managers;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Speech.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using YamlDotNet.Core.Tokens;
using System.Linq;
using Content.Server.Atmos.EntitySystems;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class MakeSyndient : EntityEffect
{
    //this is basically completely copied from MakeSentient, but with a bit of changes to how the ghost roles are listed
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-make-sentient", ("chance", Probability));

    [Dependency] private readonly ForensicsSystem _forensicsSystem = default!;

    public override void Effect(EntityEffectBaseArgs args)
    {

        var entityManager = args.EntityManager;
        var uid = args.TargetEntity;
        

        // Let affected entities speak normally to make this effect different from, say, the "random sentience" event
        // This also works on entities that already have a mind
        // We call this before the mind check to allow things like player-controlled mice to be able to benefit from the effect
        entityManager.RemoveComponent<ReplacementAccentComponent>(uid);
        entityManager.RemoveComponent<MonkeyAccentComponent>(uid);

        // Stops from adding a ghost role to things like people who already have a mind
        if (entityManager.TryGetComponent<MindContainerComponent>(uid, out var mindContainer) && mindContainer.HasMind)
        {
            return;
        }

        //slightly hacky way to make sure it doesn't work on humanoid ghost roles that haven't been claimed yet
        if (entityManager.TryGetComponent<HumanoidAppearanceComponent>(uid, out HumanoidAppearanceComponent? component))
        {
            return;
        }
        var forensicSys = args.EntityManager.System<ForensicsSystem>();

        //hide your children, it's time to figure out whose blood is in this shit
        if (args is EntityEffectReagentArgs reagentArgs)
        {
            //get all DNAs stored in the injected solution
            List<DnaData> dnaDataList = new List<DnaData>();
            if (reagentArgs.Source != null)
            {
                foreach (var reagent in reagentArgs.Source.Contents)
                {
                    foreach (var data in reagent.Reagent.EnsureReagentData())
                    {
                        if (data is DnaData)
                        {
                            dnaDataList.Add(((DnaData)data));
                        }
                    }
                }

                String? chosenName = null;

                //we have all the DNA in the activated subjuzine. get a random one and find the DNA's source.
                for (int i=0; i<dnaDataList.Count; i++)
                {
                    DnaData candidate = dnaDataList[i];
                    String? candidateName = forensicSys.GetNameFromDNA(candidate.DNA);

                    if (candidateName != null)
                    {
                        chosenName = candidateName;
                    }
                }

                if (chosenName!=null)
                {
                    //we FINALLY have the name of the injector. jesus fuck.
                    //now, we build the role name, description, etc.

                    //Don't add a ghost role to things that already have ghost roles

                    String rules = (Loc.GetString("ghost-role-information-subjuzine-rules-1"));
                    rules = rules + chosenName;
                    rules = rules + (Loc.GetString("ghost-role-information-subjuzine-rules-2"));

                    if (entityManager.TryGetComponent(uid, out GhostRoleComponent? ghostRole))
                    {
                        //if there already was a ghost role, change the role description and rules to make it clear it's been injected with subjuzine
                        ghostRole = entityManager.GetComponent<GhostRoleComponent>(uid);
                        ghostRole.RoleDescription = Loc.GetString("ghost-role-information-subjuzine-description");
                        ghostRole.RoleRules = rules;
                        return; 
                    }

                    ghostRole = entityManager.AddComponent<GhostRoleComponent>(uid);
                    entityManager.EnsureComponent<GhostTakeoverAvailableComponent>(uid);

                    var entityData = entityManager.GetComponent<MetaDataComponent>(uid);
                    ghostRole.RoleName = entityData.EntityName;
                    ghostRole.RoleDescription = Loc.GetString("ghost-role-information-subjuzine-description");
                    ghostRole.RoleRules = rules;


                }
                else //if there's no DNA in the DNA list, just act as if it was normal cognizine.
                {
                    //Don't add a ghost role to things that already have ghost roles
                    if (entityManager.TryGetComponent(uid, out GhostRoleComponent? ghostRole))
                    {
                        return;
                    }

                    ghostRole = entityManager.AddComponent<GhostRoleComponent>(uid);
                    entityManager.EnsureComponent<GhostTakeoverAvailableComponent>(uid);

                    var entityData = entityManager.GetComponent<MetaDataComponent>(uid);
                    ghostRole.RoleName = entityData.EntityName;
                    ghostRole.RoleDescription = Loc.GetString("ghost-role-information-cognizine-description");
                }
            }
        }
    }
}
