using System.Linq;
using Content.Server.Storage.Components;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Storage;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server.Storage.EntitySystems;

// TODO: move this to shared for verb prediction if/when storage is in shared
public sealed class PickRandomSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PickRandomComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs);
    }

    private void OnGetAlternativeVerbs(EntityUid uid, PickRandomComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !TryComp<StorageComponent>(uid, out var storage))
            return;

        var user = args.User;

        var enabled = storage.Container.ContainedEntities.Any(item => _whitelistSystem.IsWhitelistPassOrNull(comp.Whitelist, item));

        // alt-click / alt-z to pick an item
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () =>
            {
                TryPick(uid, comp, storage, user);
            },
            Impact = LogImpact.Low,
            Text = Loc.GetString(comp.VerbText),
            Disabled = !enabled,
            Message = enabled ? null : Loc.GetString(comp.EmptyText, ("storage", uid))
        });
    }

    private void TryPick(EntityUid uid, PickRandomComponent comp, StorageComponent storage, EntityUid user)
    {
        var entities = storage.Container.ContainedEntities.Where(item => _whitelistSystem.IsWhitelistPassOrNull(comp.Whitelist, item)).ToArray();

        if (!entities.Any())
            return;

        var picked = _random.Pick(entities);
        // if it fails to go into a hand of the user, will be on the storage
        _container.AttachParentToContainerOrGrid((picked, Transform(picked)));

        // TODO: try to put in hands, failing that put it on the storage
        _hands.TryPickupAnyHand(user, picked);
    }
}
