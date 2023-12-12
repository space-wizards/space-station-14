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
using Robust.Shared.Random;

namespace Content.Client.Eye.Blinding;

public sealed class ChromaticAberrationSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;


    private ChromaticAberrationOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChromaticAberrationComponent, ComponentStartup>(OnChromaticAberrationStartup);
        SubscribeLocalEvent<ChromaticAberrationComponent, ComponentShutdown>(OnChromaticAberrationShutdown);

        SubscribeLocalEvent<ChromaticAberrationComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ChromaticAberrationComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<ChromaticAberrationComponent, DidEquipEvent>(DidEquip);
        SubscribeLocalEvent<ChromaticAberrationComponent, DidUnequipEvent>(DidUnequip);

        _overlay = new();
    }
	
	private void OnPlayerAttached(EntityUid uid, ChromaticAberrationComponent component, LocalPlayerAttachedEvent args)
    {
        UpdateShader(component);
    }

    private void OnPlayerDetached(EntityUid uid, ChromaticAberrationComponent component, LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnChromaticAberrationStartup(EntityUid uid, ChromaticAberrationComponent component, ComponentStartup args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
			UpdateShader(component);
    }

    private void OnChromaticAberrationShutdown(EntityUid uid, ChromaticAberrationComponent component, ComponentShutdown args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
        {
            _overlayMan.RemoveOverlay(_overlay);
        }
    }
	
	private void DidEquip(EntityUid uid, ChromaticAberrationComponent component, DidEquipEvent args)
    {
        var comp = EnsureComp<TagComponent>(args.Equipment);

        if (comp.Tags.Contains("GlassesChromaticAberration") && args.SlotFlags == SlotFlags.EYES) UpdateShaderGlasses(component);
        else if (args.SlotFlags is not SlotFlags.EYES) UpdateShader(component);
    }
	
	private void DidUnequip(EntityUid uid, ChromaticAberrationComponent component, DidUnequipEvent args)
    {
        UpdateShader(component);
    }
	
	private float[][] ExtractMatrix(ChromaticAberrationComponent component)
	{
		return new float[][] {
			new float[] {component.A1, component.A2, component.A3},
			new float[] {component.B1, component.B2, component.B3},
			new float[] {component.C1, component.C2, component.C3}};
	}
	
	private void UpdateShader(ChromaticAberrationComponent component)
    {
        _overlayMan.RemoveOverlay(_overlay);
        _overlay.AlphaValue = component.Alpha;
		_overlay.Matr = ExtractMatrix(component);
        _overlayMan.AddOverlay(_overlay);
    }

    private void UpdateShaderGlasses(ChromaticAberrationComponent component)
    {
        _overlayMan.RemoveOverlay(_overlay);
        _overlay.AlphaValue = component.AlphaGlasses;
		_overlay.Matr = ExtractMatrix(component);
        _overlayMan.AddOverlay(_overlay);
    }
}
