using Content.Shared._Starlight.Railroading;
using Content.Shared._Starlight.Railroading.Events;
using Content.Server.Access.Systems;
using Robust.Shared.Prototypes;

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
        if (!_idCard.TryFindIdCard(args.Subject, out var idCard)) return;

        if (comp.Title != null) _idCard.TryChangeJobTitle(idCard.Owner, comp.Title);
        if (comp.Icon != null) _idCard.TryChangeJobIcon(idCard.Owner, _prototypeManager.Index(comp.Icon));
        if (comp.Name != null) _idCard.TryChangeFullName(idCard.Owner, comp.Name);
    }
}