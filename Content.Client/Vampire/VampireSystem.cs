using System.Linq;
using Content.Client.Alerts;
using Content.Client.UserInterface.Systems.Alerts.Controls;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Vampire;
using Content.Shared.Vampire.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Vampire;

public sealed class VampireSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireIconComponent, GetStatusIconsEvent>(GetVampireIcon);
        SubscribeLocalEvent<VampireAlertComponent, UpdateAlertSpriteEvent>(OnUpdateAlert);
    }
    
    private void GetVampireIcon(EntityUid uid, VampireIconComponent component, ref GetStatusIconsEvent args)
    {
        var iconPrototype = _prototype.Index(component.StatusIcon);
        args.StatusIcons.Add(iconPrototype);
    }
    
    private void OnUpdateAlert(EntityUid uid, VampireAlertComponent component, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.ID != component.BloodAlert)
            return;
        
        var sprite = args.SpriteViewEnt.Comp;
        var blood = Math.Clamp(component.BloodAmount, 0, 999);
        sprite.LayerSetState(VampireVisualLayers.Digit1, $"{(blood / 100) % 10}");
        sprite.LayerSetState(VampireVisualLayers.Digit2, $"{(blood / 10) % 10}");
        sprite.LayerSetState(VampireVisualLayers.Digit3, $"{blood % 10}");
    }
}