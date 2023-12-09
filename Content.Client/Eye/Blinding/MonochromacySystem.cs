using Content.Shared.Eye.Blinding;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Content.Shared.Eye.Blinding.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Content.Shared.Inventory.Events;
using Content.Shared.Tag;
using Content.Shared.Inventory;
using Content.Client.Inventory;

namespace Content.Client.Eye.Blinding;

public sealed class MonochromacySystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;


    private MonochromacyOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MonochromacyComponent, ComponentStartup>(OnMonochromacyStartup);
        SubscribeLocalEvent<MonochromacyComponent, ComponentShutdown>(OnMonochromacyShutdown);

        SubscribeLocalEvent<MonochromacyComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<MonochromacyComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<MonochromacyComponent, DidEquipEvent>(DidEquip);
        SubscribeLocalEvent<MonochromacyComponent, DidUnequipEvent>(DidUnequip);

        _overlay = new();
    }

    private void OnPlayerAttached(EntityUid uid, MonochromacyComponent component, LocalPlayerAttachedEvent args)
    {
        UpdateShader(component);
    }

    private void OnPlayerDetached(EntityUid uid, MonochromacyComponent component, LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnMonochromacyStartup(EntityUid uid, MonochromacyComponent component, ComponentStartup args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
			UpdateShader(component);
    }

    private void OnMonochromacyShutdown(EntityUid uid, MonochromacyComponent component, ComponentShutdown args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
        {
            _overlayMan.RemoveOverlay(_overlay);
        }
    }
	
	private void DidEquip(EntityUid uid, MonochromacyComponent component, DidEquipEvent args)
    {
        var comp = EnsureComp<TagComponent>(args.Equipment);

        if (comp.Tags.Contains("GlassesMonochromacy") && args.SlotFlags == SlotFlags.EYES) UpdateShaderGlasses(component);
        else if (args.SlotFlags is not SlotFlags.EYES) UpdateShader(component);
    }
	
	private void DidUnequip(EntityUid uid, MonochromacyComponent component, DidUnequipEvent args)
    {
        UpdateShader(component);
    }
	
	private void UpdateShader(MonochromacyComponent component)
    {
        _overlayMan.RemoveOverlay(_overlay);
        _overlay.AlphaValue = component.Alpha;
        _overlayMan.AddOverlay(_overlay);
    }

    private void UpdateShaderGlasses(MonochromacyComponent component)
    {
        _overlayMan.RemoveOverlay(_overlay);
        _overlay.AlphaValue = component.AlphaGlasses;
        _overlayMan.AddOverlay(_overlay);
    }
}
