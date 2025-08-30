using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Content.Client.UserInterface.Controls;
using System.Numerics;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;
using Content.Client.Mech.Ui;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameStates;

namespace Content.Client.Mech;

/// <inheritdoc/>
public sealed class MechSystem : SharedMechSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechComponent, AppearanceChangeEvent>(OnAppearanceChanged);
        SubscribeLocalEvent<MechPilotComponent, PrepareMeleeLungeEvent>(OnPrepareMeleeLunge);
        SubscribeLocalEvent<MechComponent, PrepareMeleeLungeEvent>(OnPrepareMeleeLunge);
        SubscribeLocalEvent<MechPilotComponent, GetMeleeAttackerEntityEvent>(OnGetMeleeAttacker);
    }

    private void OnAppearanceChanged(EntityUid uid, MechComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var state = component.BaseState;
        var drawDepth = DrawDepth.Mobs;

        var isBroken = false;
        var isOpen = false;

        if (args.AppearanceData.TryGetValue(MechVisuals.Broken, out var brokenObj) && brokenObj is bool brokenFlag)
            isBroken = brokenFlag;
        if (args.AppearanceData.TryGetValue(MechVisuals.Open, out var openObj) && openObj is bool openFlag)
            isOpen = openFlag;

        // Priority: Broken > Open > Base
        if (component.BrokenState != null && isBroken)
        {
            state = component.BrokenState;
            drawDepth = DrawDepth.SmallMobs;
        }
        else if (component.OpenState != null && isOpen)
        {
            state = component.OpenState;
            drawDepth = DrawDepth.SmallMobs;
        }

        _sprite.LayerSetVisible((uid, args.Sprite), MechVisualLayers.Base, true);
        _sprite.LayerSetAutoAnimated((uid, args.Sprite), MechVisualLayers.Base, true);
        _sprite.LayerSetRsiState((uid, args.Sprite), MechVisualLayers.Base, state);
        _sprite.SetDrawDepth((uid, args.Sprite), (int)drawDepth);
    }

    private void OnPrepareMeleeLunge(EntityUid uid, MechPilotComponent comp, ref PrepareMeleeLungeEvent args)
    {
        args.SpawnAtMap = true;
        args.DisableTracking = true;
    }

    private void OnPrepareMeleeLunge(EntityUid uid, MechComponent comp, ref PrepareMeleeLungeEvent args)
    {
        args.SpawnAtMap = true;
        args.DisableTracking = true;
    }

    private void OnGetMeleeAttacker(EntityUid uid, MechPilotComponent comp, ref GetMeleeAttackerEntityEvent args)
    {
        if (args.Handled)
            return;

        args.Attacker = comp.Mech;
        args.Handled = true;
    }
}
