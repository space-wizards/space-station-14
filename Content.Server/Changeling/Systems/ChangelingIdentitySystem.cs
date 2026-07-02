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
        List<ChangelingNetworkedIdentityData> sentIdentities = new();

        foreach (var identity in entity.Comp.ConsumedIdentities)
        {
            ChangelingNetworkedIdentityData netData = new()
            {
                Identity = GetNetEntity(identity.Identity),
                Original = GetNetEntity(identity.Original),
                OriginalJob = identity.OriginalJob,
                OriginalName = identity.OriginalName,
                Starting = identity.Starting,
                GrantedDna = identity.GrantedDna,
            };

            sentIdentities.Add(netData);
        }

        var current = entity.Comp.CurrentIdentity;

        var netCurrent = GetNetEntity(current);

        args.State = new ChangelingIdentityComponentState(
            sentIdentities,
            netCurrent,
            entity.Comp.IdentityCloningSettings,
            entity.Comp.MaxStoredDisguises);
    }
}
