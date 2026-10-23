using Content.Server.Objectives.Components;
using Content.Server.Traitor.Components;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// This handles the blacklisting of certain traitor objectives from certain traitor profiles. <br/>
/// Can be used to prevent syndie reinforcements from being given certain objectives, and vice versa.
/// </summary>
public sealed partial class TraitorProfileBlacklistSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TraitorProfileBlacklistComponent, RequirementCheckEvent>(OnCheck);
    }

    private void OnCheck(EntityUid uid, TraitorProfileBlacklistComponent comp, ref RequirementCheckEvent args)
    {
        if (args.Cancelled)
            return;
        
        if (!args.Mind.OwnedEntity.HasValue)
        {
            return;
        }

        foreach (var traitorComp in AllComps<AutoTraitorComponent>(args.Mind.OwnedEntity.Value))
        {
            foreach (var blacklistedProfile in comp.Profiles)
            {
                if (!blacklistedProfile.Equals(traitorComp.Profile)) continue;
                Log.Debug($"Profile {traitorComp.Profile} is blacklisted by this objective");
                args.Cancelled = true;
                return;

            }
        }
    }
}