using System.Linq;
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

        SubscribeLocalEvent<ChangelingIdentityComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(Entity<ChangelingIdentityComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not ChangelingIdentityComponentState state)
            return;

        ent.Comp.ConsumedIdentities = new List<ChangelingIdentityData>();

        foreach (var identities in state.ConsumedIdentities)
        {
            ChangelingIdentityData data = new();

            data.Identity = GetEntity(identities.Identity);
            data.Original = GetEntity(identities.Original);
            data.OriginalMind = null; // Don't network the mind!
            data.OriginalJob = identities.OriginalJob;
            data.GrantedDna = identities.GrantedDna;

            ent.Comp.ConsumedIdentities.Add(data);
        }

        ent.Comp.CurrentIdentity = GetEntity(state.CurrentIdentity);

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
