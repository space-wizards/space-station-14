// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using System;
using System.Collections.Generic;
using Content.Shared.BloodCult;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Map;
using TimedDespawnComponent = Robust.Shared.Spawners.TimedDespawnComponent;

namespace Content.Client.BloodCult.Systems;

public sealed class BloodCultRuneEffectSystem : EntitySystem
{
    private readonly Dictionary<uint, EntityUid> _activeEffects = new();

    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RuneDrawingEffectEvent>(OnRuneEffectEvent);
    }

    private void OnRuneEffectEvent(RuneDrawingEffectEvent ev)
    {
        var coordinates = GetCoordinates(ev.Coordinates);

        switch (ev.Action)
        {
            case RuneEffectAction.Start:
                HandleStart(ev, coordinates);
                break;
            case RuneEffectAction.Stop:
                HandleStop(ev);
                break;
        }
    }

    private void HandleStart(RuneDrawingEffectEvent ev, EntityCoordinates coordinates)
    {
        if (string.IsNullOrEmpty(ev.Prototype))
            return;

        if (_activeEffects.TryGetValue(ev.EffectId, out var existing) && !Deleted(existing))
            QueueDel(existing);

        var effect = Spawn(ev.Prototype, coordinates);

        if (!TryComp<SpriteComponent>(effect, out var sprite))
        {
            _activeEffects.Remove(ev.EffectId);
            return;
        }

        var spriteEntity = new Entity<SpriteComponent?>(effect, sprite);

        // Enable normal sprite animation - the _drawing sprite will play its animation
        if (_sprite.TryGetLayer(spriteEntity, EffectLayers.Unshaded, out var layer, false))
        {
            _sprite.LayerSetAutoAnimated(spriteEntity, EffectLayers.Unshaded, true);
            _sprite.LayerSetAnimationTime(spriteEntity, EffectLayers.Unshaded, 0f);
        }

        // Ensure the effect lasts at least as long as the duration
        if (TryComp(effect, out TimedDespawnComponent? despawn))
        {
            var desiredLifetime = (float) ev.Duration.TotalSeconds + 0.5f;
            if (despawn.Lifetime < desiredLifetime)
                despawn.Lifetime = desiredLifetime;
        }

        _activeEffects[ev.EffectId] = effect;
    }

    private void HandleStop(RuneDrawingEffectEvent ev)
    {
        if (!_activeEffects.TryGetValue(ev.EffectId, out var effectUid))
            return;

        _activeEffects.Remove(ev.EffectId);

        if (Deleted(effectUid))
            return;

        if (!TryComp<SpriteComponent>(effectUid, out var sprite))
        {
            QueueDel(effectUid);
            return;
        }

        var spriteEntity = new Entity<SpriteComponent?>(effectUid, sprite);

        // Transition from _drawing state to normal state
        if (_sprite.LayerMapTryGet(spriteEntity, EffectLayers.Unshaded, out var layerIndex, false))
        {
            var currentState = _sprite.LayerGetRsiState(spriteEntity, layerIndex);
            var currentStateName = currentState.Name ?? "";
            
            // If the state ends with "_drawing", change it to the normal state
            if (currentStateName.EndsWith("_drawing"))
            {
                var normalState = currentStateName.Substring(0, currentStateName.Length - "_drawing".Length);
                _sprite.LayerSetRsiState(spriteEntity, EffectLayers.Unshaded, new RSI.StateId(normalState));
            }
        }

        QueueDel(effectUid);
    }
}
