// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Movement.Systems;
using Content.Shared.DeadSpace.Demons.DemonShadow.Components;
using Content.Shared.Popups;

namespace Content.Shared.DeadSpace.Demons.DemonShadow;

public abstract class SharedDemonShadowSystem : EntitySystem
{
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DemonShadowComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
    }

    private void OnRefresh(EntityUid uid, DemonShadowComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.MovementSpeedMultiply, component.MovementSpeedMultiply);
    }
}
