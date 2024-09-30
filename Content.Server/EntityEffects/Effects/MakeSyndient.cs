using Content.Server.Forensics;
using Content.Server.Chat.Managers;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Speech.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Robust.Shared.Prototypes;
using YamlDotNet.Core.Tokens;
using System.Linq;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class MakeSyndient : EntityEffect
{
    //this is basically completely copied from MakeSentient, but with a bit of changes to how the ghost roles are listed
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-make-sentient", ("chance", Probability));

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;
        var uid = args.TargetEntity;
        var EntityQueryEnumerator query = EntityQueryEnumerator<DnaComponent>();

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
                //we have all the DNA in the activated subjuzine. get a random one and find the DNA's source.
                if (dnaDataList.Count > 0)
                {
                    Random r = new Random();
                    DnaData chosenOne = dnaDataList[r.Next(0, dnaDataList.Count)];

                    //store the chosen one's name for later use in the welcome message.
                    String chosenName = "OH GOD OH FUCK IT'S BROKEN";
                    //iterate over every DNAcomponent in the server until you find one that matches the given DNA
                    while (query.MoveNext(out var sourceUID, out var sourceComp))
                    {
                        if (sourceComp.DNA.Equals(chosenOne.DNA)){

                            if(entityManager.TryGetComponent(sourceUID, out MetaDataComponent? metaData))
                            {
                                chosenName = metaData.EntityName;
                            }
                        }
                    }
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
