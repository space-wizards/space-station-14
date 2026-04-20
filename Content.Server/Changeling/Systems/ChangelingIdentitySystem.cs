using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Systems;
using Robust.Shared.GameStates;

namespace Content.Server.Changeling.Systems;

public sealed class ChangelingIdentitySystem : SharedChangelingIdentitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingIdentityComponent, ComponentGetState>(OnGetState);
    }

    private void OnGetState(Entity<ChangelingIdentityComponent> entity, ref ComponentGetState args)
    {
        HashSet<ChangelingNetworkedIdentityData> sentIdentities = new();

        foreach (var identity in entity.Comp.ConsumedIdentities)
        {
            ChangelingNetworkedIdentityData netData = new();

            netData.Identity = GetNetEntity(identity.Identity);
            netData.Original = GetNetEntity(identity.Original);
            netData.GrantedDna = identity.GrantedDna;
            netData.OriginalJob = identity.OriginalJob;

            sentIdentities.Add(netData);
        }

        var current = entity.Comp.CurrentIdentity;

        ChangelingNetworkedIdentityData? netCurrent;

        if (current != null)
        {
            netCurrent = new ChangelingNetworkedIdentityData();

            netCurrent.Identity = GetNetEntity(current.Identity);
            netCurrent.Original = GetNetEntity(current.Original);
            netCurrent.GrantedDna = current.GrantedDna;
            netCurrent.OriginalJob = current.OriginalJob;
        }
        else
        {
            netCurrent = null;
        }

        args.State = new ChangelingIdentityComponentState(sentIdentities, netCurrent, entity.Comp.IdentityCloningSettings);
    }
}
