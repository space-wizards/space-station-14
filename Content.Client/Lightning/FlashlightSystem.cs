using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Ghost;
using Content.Client.Hands.Systems;
using Content.Client.Inventory;
using Content.Client.Popups;
using Content.Shared.Input;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Content.Shared.Popups;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;

namespace Content.Client.Lightning;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class FlashlightSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private ClientInventorySystem _inventory = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private GhostSystem _ghost = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleFlashlight, new PointerInputCmdHandler(OnToggleFlashlightPressed, outsidePrediction: true))
            .Register<FlashlightSystem>();
    }

    private bool OnToggleFlashlightPressed(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (_player.LocalEntity is not { } player)
            return false;

        if (_ghost.IsGhost)
        {
            _ghost.ToggleGhostLight(player);
            return true;
        }

        if (TryGetFlashlight(out var light))
        {
            SendToggleMessage(light.Value);
        }
        else
        {
            _popup.PopupCursor(Loc.GetString("flashlight-popup-no-flashlight"), player, PopupType.Medium);
        }
        return true;
    }

    public bool TryGetFlashlight([NotNullWhen(true)] out EntityUid? outLight)
    {
        outLight = null;
        if (_player.LocalEntity is not { } player)
            return false;

        // Find candidate lights
        var lights = new HashSet<(EntityUid Uid, bool On)>();
        var slotEnumerator = _inventory.GetSlotEnumerator(player);
        while (slotEnumerator.MoveNext(out var inventorySlot))
        {
            if (inventorySlot.ContainedEntity is not { } entity)
                continue;

            // CHECK BOTH FLASHLIGHTS, FOR SOME REASON
            if (TryComp<HandheldLightComponent>(entity, out var handheld))
            {
                lights.Add((entity, handheld.Activated));
            }
            else if (TryComp<UnpoweredFlashlightComponent>(entity, out var flashlight))
            {
                lights.Add((entity, flashlight.LightOn));
            }
        }

        foreach (var handId in _hands.EnumerateHands(player))
        {
            if (!_hands.TryGetHeldItem(player, handId, out var held))
                continue;

            // CHECK BOTH FLASHLIGHTS, FOR SOME REASON
            if (TryComp<HandheldLightComponent>(held, out var handheld))
            {
                lights.Add((held.Value, handheld.Activated));
            }
            else if (TryComp<UnpoweredFlashlightComponent>(held, out var flashlight))
            {
                lights.Add((held.Value, flashlight.LightOn));
            }
        }

        {
            if (TryComp<HandheldLightComponent>(player, out var handheld))
            {
                lights.Add((player, handheld.Activated));
            }
            else if (TryComp<UnpoweredFlashlightComponent>(player, out var flashlight))
            {
                lights.Add((player, flashlight.LightOn));
            }
        }

        if (lights.Count == 0)
            return false;

        if (lights.FirstOrNull(pair => pair.On) is { } light)
        {
            outLight = light.Uid;
            return true;
        }

        outLight = lights.MaxBy(pair => EntityManager.GetComponentOrNull<PointLightComponent>(pair.Uid)?.Radius ?? 0).Uid;
        return true;
    }

    private void SendToggleMessage(EntityUid uid)
    {
        var netEnt = GetNetEntity(uid);
        RaisePredictiveEvent(new ToggleFlashlightEvent(netEnt));
    }
}
