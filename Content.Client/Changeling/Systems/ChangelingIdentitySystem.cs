using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.Changeling.Systems;

public sealed class ChangelingIdentitySystem : SharedChangelingIdentitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingIdentityComponent, ComponentHandleState>(OnAfterAutoHandleState);
    }

    private void OnAfterAutoHandleState(Entity<ChangelingIdentityComponent> ent, ref ComponentHandleState args)
    {
        if (args.Next is not ChangelingIdentityComponentState state)
            return;

        ent.Comp.ConsumedIdentities = new HashSet<ChangelingIdentityData>();

        foreach (var identities in state.ConsumedIdentities)
        {
            ChangelingIdentityData data = new();

            data.Identity = GetEntity(identities.Identity);
            data.Original = GetEntity(identities.Original);
            data.OriginalMind = null;
            data.OriginalJob = identities.OriginalJob;
            data.GrantedDna = identities.GrantedDna;

            ent.Comp.ConsumedIdentities.Add(data);
        }

        if (state.CurrentIdentity == null)
        {
            ent.Comp.CurrentIdentity = null;
        }
        else
        {
            ent.Comp.CurrentIdentity = new ChangelingIdentityData();
            ent.Comp.CurrentIdentity.Identity = GetEntity(state.CurrentIdentity.Identity);
            ent.Comp.CurrentIdentity.Original = GetEntity(state.CurrentIdentity.Identity);
            ent.Comp.CurrentIdentity.OriginalMind = null;
            ent.Comp.CurrentIdentity.GrantedDna = state.CurrentIdentity.GrantedDna;
            ent.Comp.CurrentIdentity.OriginalJob = state.CurrentIdentity.OriginalJob;
        }

        ent.Comp.IdentityCloningSettings = state.IdentityCloningSettings;

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
