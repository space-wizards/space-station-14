using System.Linq;
using Content.Client.Overlays;
using Content.Shared._Impstation.SalvoHud;
using Content.Shared.Inventory;
using Content.Shared.Materials;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Impstation.SalvoHud;

/// <summary>
/// This system adds status icons to entities with a PhysicalComposition component. It also does a whole host of other bullshit that I hate.
/// </summary>
public sealed class ShowMaterialCompositionIconsSystem : EquipmentHudSystem<ShowMaterialCompositionIconsComponent>
{

    //todo fixedPrice showing as well... later
    //god I hate all of this code so much I have no idea why it was so painful to write

    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;

    private SalvoHudScanOverlay _overlay = default!;

    //yaaay storing state in a system
    //makes things easier + means I'm not re-getting the salvohud ent for every status icon
    private ShowMaterialCompositionIconsComponent? _iconsComp;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhysicalCompositionComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);

        if (_overlayMan.TryGetOverlay<SalvoHudScanOverlay>(out var overlay))
        {
            _overlay = overlay;
        }
        else
        {
            _overlay = new SalvoHudScanOverlay();
            _overlayMan.AddOverlay(_overlay);
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        //play the hits
        //needs to be here because I'm using an accumulator instead of timestamps, probably.
        //also actively doesn't need prediction because this is all clientside. checkmate woke moralists.
        if (!_timing.IsFirstTimePredicted)
            return;

        //update salvohuds
        //don't really care if they're out of sync w/ the server because their stuff only ever gets shown clientside?
        //can fix later if it's an issue
        var query = EntityQueryEnumerator<ShowMaterialCompositionIconsComponent>();
        while (query.MoveNext(out var comp))
        {
            switch (comp.CurrState)
            {
                case SalvohudScanState.Idle:
                    //constantly reset to a known "empty" state if idle
                    comp.CurrRadius = 0;
                    comp.CurrMinRadius = 0;
                    comp.Accumulator = 0;
                    comp.LastPingPos = null;
                    break;

                case SalvohudScanState.In:
                    comp.Accumulator += frameTime;
                    var inProgress = comp.Accumulator / comp.InPeriod;
                    comp.CurrRadius = comp.MaxRadius * inProgress;

                    if (comp.Accumulator < comp.InPeriod)
                        break;

                    comp.Accumulator = 0;
                    comp.CurrState = SalvohudScanState.Active;
                    break;

                case SalvohudScanState.Active:
                    comp.Accumulator += frameTime;
                    comp.CurrRadius = comp.MaxRadius;

                    if (comp.Accumulator < comp.ActivePeriod)
                        break;

                    comp.Accumulator = 0;
                    comp.CurrState = SalvohudScanState.Out;
                    break;

                case SalvohudScanState.Out:
                    comp.Accumulator += frameTime;
                    var outProgress = comp.Accumulator / comp.OutPeriod;
                    comp.CurrMinRadius = comp.MaxRadius * outProgress;

                    if (comp.Accumulator < comp.OutPeriod)
                        break;

                    comp.Accumulator = 0;
                    comp.CurrState = SalvohudScanState.Idle;
                    break;
            }
        }

        //vaguely evil thing to get the salvohud the player is currently wearing, if any
        _iconsComp = null;
        if (_playerMan.LocalEntity is not {} player || !TryComp<InventoryComponent>(player, out var inventoryComp))
            return;

        var invEnumerator = new InventorySystem.InventorySlotEnumerator(inventoryComp, SlotFlags.EYES);
        while (invEnumerator.MoveNext(out var inventorySlot))
        {
            //if the player somehow has multiple eye slots, only get the last salvohud
            if (inventorySlot.ContainedEntity != null && HasComp<ShowMaterialCompositionIconsComponent>(inventorySlot.ContainedEntity))
                _iconsComp = Comp<ShowMaterialCompositionIconsComponent>(inventorySlot.ContainedEntity.Value);
        }

        if (_iconsComp == null)
        {
            //more sate resetting
            _overlay.Radius = 0f;
            _overlay.ScanPoint = null;
        }
        else
        {
            if (_iconsComp.CurrState == SalvohudScanState.In) //this feels like it sucks but kinda doesn't? idk but I kinda hate all of this code.
            {
                _overlay.ScanPoint ??= _iconsComp.LastPingPos;
                _overlay.Alpha = 1f - _iconsComp.Accumulator;
            }
            else
            {
                _overlay.Alpha = 0f;
            }

            _overlay.Radius = _iconsComp.CurrRadius;
            _overlay.ScanPoint = _iconsComp.LastPingPos;
        }
    }

    private void OnGetStatusIconsEvent(Entity<PhysicalCompositionComponent> entity, ref GetStatusIconsEvent args)
    {
        if (!IsActive || _iconsComp == null || _iconsComp.CurrState == SalvohudScanState.Idle || _iconsComp.LastPingPos == null)
            return;

        var diff = _xform.GetWorldPosition(entity) - _iconsComp.LastPingPos;
        var dist = diff.Value.Length();

        if (dist > _iconsComp.CurrRadius || dist < _iconsComp.CurrMinRadius)
            return;

        //sort material comp by value so things w/ more than 4 mats get the 4 highest prio mats shown
        foreach (var (id, _) in entity.Comp.MaterialComposition.OrderByDescending(x => x.Value))
        {
            if (_protoMan.TryIndex<MaterialCompositionIconPrototype>(id, out var proto))
                args.StatusIcons.Add(proto);
        }
    }
}
