using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Systems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Mech;

public sealed class MechSystem : SharedMechSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechComponent, MechToggleEquipmentEvent>(OnToggleEquipmentAction);
        SubscribeLocalEvent<MechComponent, AppearanceChangeEvent>(OnAppearanceChanged);
        SubscribeLocalEvent<MechComponent, PrepareMeleeLungeEvent>(OnPrepareMeleeLunge);
        SubscribeLocalEvent<MechPilotComponent, GetMeleeAttackerEntityEvent>(OnGetMeleeAttacker);
    }

    private void OnToggleEquipmentAction(Entity<MechComponent> ent, ref MechToggleEquipmentEvent args)
    {
        if (args.Handled)
            return;

        RaiseLocalEvent(ent.Owner, new MechOpenEquipmentRadialEvent());
        args.Handled = true;
    }

    private void OnAppearanceChanged(Entity<MechComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var state = ent.Comp.BaseState;
        var drawDepth = DrawDepth.Mobs;

        var isBroken = false;
        var isOpen = false;

        if (args.AppearanceData.TryGetValue(MechVisuals.Broken, out var brokenObj) && brokenObj is bool brokenFlag)
            isBroken = brokenFlag;
        if (args.AppearanceData.TryGetValue(MechVisuals.Open, out var openObj) && openObj is bool openFlag)
            isOpen = openFlag;

        // Priority: Broken > Open > Base
        if (ent.Comp.BrokenState != null && isBroken)
        {
            state = ent.Comp.BrokenState;
            drawDepth = DrawDepth.SmallMobs;
        }
        else if (ent.Comp.OpenState != null && isOpen)
        {
            state = ent.Comp.OpenState;
            drawDepth = DrawDepth.SmallMobs;
        }

        _sprite.LayerSetVisible((ent.Owner, args.Sprite), MechVisualLayers.Base, true);
        _sprite.LayerSetAutoAnimated((ent.Owner, args.Sprite), MechVisualLayers.Base, true);
        _sprite.LayerSetRsiState((ent.Owner, args.Sprite), MechVisualLayers.Base, state);
        _sprite.SetDrawDepth((ent.Owner, args.Sprite), (int)drawDepth);
    }

    private static void OnPrepareMeleeLunge(Entity<MechComponent> ent, ref PrepareMeleeLungeEvent args)
    {
        args.SpawnAtMap = true;
        args.DisableTracking = true;
    }

    private static void OnGetMeleeAttacker(Entity<MechPilotComponent> ent, ref GetMeleeAttackerEntityEvent args)
    {
        if (args.Handled)
            return;

        args.Attacker = ent.Comp.Mech;
        args.Handled = true;
    }
}
