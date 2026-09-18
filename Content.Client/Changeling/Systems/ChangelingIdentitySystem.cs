using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.Changeling.Systems;

public sealed partial class ChangelingIdentitySystem : SharedChangelingIdentitySystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingIdentityComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(Entity<ChangelingIdentityComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not ChangelingIdentityComponentState state)
            return;

        ent.Comp.ConsumedIdentities = new List<ChangelingIdentityData>();

        foreach (var identity in state.ConsumedIdentities)
        {
            ChangelingIdentityData data = new()
            {
                Identity = EnsureEntity<ChangelingIdentityComponent>(identity.Identity, ent),
                Original = EnsureEntity<ChangelingIdentityComponent>(identity.Original, ent),
                OriginalMind = null, // Don't network the mind!
                OriginalJob = identity.OriginalJob,
                OriginalName = identity.OriginalName,
                Starting = identity.Starting,
                GrantedDna = identity.GrantedDna,
            };

            ent.Comp.ConsumedIdentities.Add(data);
        }

        ent.Comp.CurrentIdentity = EnsureEntity<ChangelingStoredIdentityComponent>(state.CurrentIdentity, ent);

        ent.Comp.IdentityCloningSettings = state.IdentityCloningSettings;
        ent.Comp.MaxStoredDisguises = state.MaxStoredDisguises;

        UpdateUi(ent);
    }

    public void UpdateUi(EntityUid uid)
    {
        if (_ui.TryGetOpenUi(uid, ChangelingTransformUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }
}
