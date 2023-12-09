using Content.Shared.Eye.Blinding;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Eye.Blinding.Components;
using Robust.Shared.GameObjects;
using Content.Shared.Inventory.Events;
using Content.Shared.Tag;
using Content.Shared.Inventory;
using Content.Client.Inventory;
using Robust.Shared.Network;

namespace Content.Client.Eye.Blinding;

public sealed class NearsightedSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
	[Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    private NearsightedOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
		
		SubscribeLocalEvent<NearsightedComponent, ComponentStartup>(OnNearsightedStartup);
        SubscribeLocalEvent<NearsightedComponent, ComponentShutdown>(OnNearsightedShutdown);

        SubscribeLocalEvent<NearsightedComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<NearsightedComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<NearsightedComponent, DidEquipEvent>(DidEquip);
        SubscribeLocalEvent<NearsightedComponent, DidUnequipEvent>(DidUnequip);
		
		_overlay = new();
    }

    private void OnPlayerAttached(EntityUid uid, NearsightedComponent component, LocalPlayerAttachedEvent args)
    {
        UpdateShader(component);
    }

    private void OnPlayerDetached(EntityUid uid, NearsightedComponent component, LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnNearsightedStartup(EntityUid uid, NearsightedComponent component, ComponentStartup args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
			UpdateShader(component);
    }

    private void OnNearsightedShutdown(EntityUid uid, NearsightedComponent component, ComponentShutdown args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
			_overlayMan.RemoveOverlay(_overlay);
    }
	
	private void DidEquip(EntityUid uid, NearsightedComponent component, DidEquipEvent args)
    {
        var comp = EnsureComp<TagComponent>(args.Equipment);

        if (comp.Tags.Contains("GlassesNearsight") && args.SlotFlags == SlotFlags.EYES) UpdateShaderGlasses(component);
        else if (args.SlotFlags is not SlotFlags.EYES) UpdateShader(component);
    }
	
	private void DidUnequip(EntityUid uid, NearsightedComponent component, DidUnequipEvent args)
    {
        UpdateShader(component);
    }

    private void UpdateShader(NearsightedComponent component)
    {
        _overlayMan.RemoveOverlay(_overlay);
        _overlay.OxygenLevel = component.Radius;
        _overlay.outerDarkness = component.Alpha;
        _overlayMan.AddOverlay(_overlay);
    }

    private void UpdateShaderGlasses(NearsightedComponent component)
    {
        _overlayMan.RemoveOverlay(_overlay);
        _overlay.OxygenLevel = component.gRadius;
        _overlay.outerDarkness = component.gAlpha;
        _overlayMan.AddOverlay(_overlay);
    }
}
