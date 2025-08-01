using System.Linq;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Random.Helpers;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Storage.EntitySystems;

public sealed class PickRandomSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

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

        if (entities.Length == 0)
            return;

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(user).Id });
        var rand = new System.Random(seed);

        // Prediction will remove and reinsert the picked item several times when rerolling the game state, meaning they are in different order in the container.
        // So we need to sort them before picking a random item to prevent it from mispredicting.
        Array.Sort(entities);
        var picked = entities[rand.Next(entities.Length)];

        // if it fails to go into a hand of the user, will be on the storage
        _container.AttachParentToContainerOrGrid((picked, Transform(picked)));

        // TODO: try to put in hands, failing that put it on the storage
        _hands.TryPickupAnyHand(user, picked);
    }
}
