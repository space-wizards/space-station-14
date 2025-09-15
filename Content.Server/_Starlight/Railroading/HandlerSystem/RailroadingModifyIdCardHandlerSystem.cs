using Content.Shared._Starlight.Railroading;
using Content.Shared._Starlight.Railroading.Events;
using Content.Server.Access.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.Access.Components;

namespace Content.Server._Starlight.Railroading;

public sealed partial class RailroadingModifyIdCardHandlerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RailroadModifyIdCardComponent, RailroadingCardChosenEvent>(OnChosen);
    }

    private void OnChosen(EntityUid uid, RailroadModifyIdCardComponent comp, RailroadingCardChosenEvent args)
    {
        if (_idCard.TryFindIdCard(args.Subject, out var idCard) &&
            Name(args.Subject).Equals(idCard.Comp.FullName))
        {   // ID is on their person
            ModifyId(comp, idCard.Owner);
        }
        else
        {   // ID is not on their person, search all
            var query = EntityQueryEnumerator<IdCardComponent>();
            while (query.MoveNext(out var idUid, out var idComp))
            {
                if (!Name(args.Subject).Equals(idComp.FullName)) continue;

                ModifyId(comp, idUid);
                break;
            }
        }

        // if we reach here no valid id was found
        // todo: when access grant/remove is made for this comp, if we reach here provide them
        // with an ID that has the granted perms, so that they can still play as intended
    }

    private void ModifyId(RailroadModifyIdCardComponent comp, EntityUid target)
    {
        if (comp.Title != null) _idCard.TryChangeJobTitle(target, comp.Title);
        if (comp.Icon != null)  _idCard.TryChangeJobIcon(target, _prototypeManager.Index(comp.Icon));
        if (comp.Name != null)  _idCard.TryChangeFullName(target, comp.Name);
    }
}