using Content.Shared._Starlight.Railroading;
using Content.Shared._Starlight.Railroading.Events;
using Content.Server.Access.Systems;
using Content.Server.Hands.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.Access.Components;
using System.Linq;
using Content.Shared.Access;

namespace Content.Server._Starlight.Railroading;

public sealed partial class RailroadingModifyIdCardHandlerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly AccessSystem _access = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RailroadModifyIdCardComponent, RailroadingCardChosenEvent>(OnChosen);
    }

    private void OnChosen(EntityUid uid, RailroadModifyIdCardComponent comp, RailroadingCardChosenEvent args)
    {
        EntityUid? cardUid = null;
        if (_idCard.TryFindIdCard(args.Subject, out var idCard) &&
            Name(args.Subject).Equals(idCard.Comp.FullName))
        {   // ID is on their person
            ModifyId(comp, idCard.Owner);
            cardUid = idCard.Owner;
        }
        else
        {   // ID is not on their person, search all
            var query = EntityQueryEnumerator<IdCardComponent>();
            while (query.MoveNext(out var idUid, out var idComp))
            {
                if (!Name(args.Subject).Equals(idComp.FullName)) continue;

                ModifyId(comp, idUid);
                cardUid = idUid;
                break;
            }
        }

        if (cardUid == null && comp.AccessAdd.Count > 0)
        {
            // no ID was found, but there are accesses to grant. Make an ID so that required accesses can still be granted
            cardUid = Spawn(comp.DefaultIdPrototypeIfNoneFound, Transform(args.Subject).Coordinates);
            _idCard.TryChangeFullName(cardUid.Value, Name(args.Subject));
            ModifyId(comp, cardUid.Value);
        }

        // update accesses
        if(cardUid != null) ModifyIdAccess(comp, cardUid.Value);
    }

    private void ModifyId(RailroadModifyIdCardComponent comp, EntityUid target)
    {
        if (comp.Title != null) _idCard.TryChangeJobTitle(target, comp.Title);
        if (comp.Icon != null) _idCard.TryChangeJobIcon(target, _prototypeManager.Index(comp.Icon));
        if (comp.Name != null) _idCard.TryChangeFullName(target, comp.Name);
    }

    private void ModifyIdAccess(RailroadModifyIdCardComponent comp, EntityUid target)
    {
        var tags = (_access.TryGetTags(target) ?? new List<ProtoId<AccessLevelPrototype>>()).ToList();
        var newTags = tags.Union(comp.AccessAdd.ToList());             // add new
        newTags = newTags.Except(comp.AccessRemove.ToList()).ToList(); // remove new

        _access.TrySetTags(target, newTags);
    }
}