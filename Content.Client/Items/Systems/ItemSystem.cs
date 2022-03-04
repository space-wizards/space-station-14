using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static Robust.Shared.GameObjects.SharedSpriteComponent;

namespace Content.Client.Items.Systems;

public sealed class ItemSystem : SharedItemSystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IResourceCache _resCache = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedItemComponent, GetInhandVisualsEvent>(OnGetVisuals);
    }

    #region InhandVisuals
    /// <summary>
    ///     When an items visual state changes, notify and entities that are holding this item that their sprite may need updating.
    /// </summary>
    public override void VisualsChanged(EntityUid uid, SharedItemComponent? item = null)
    {
        if (!Resolve(uid, ref item))
            return;

        // if the item is in a container, it might be equipped to hands or inventory slots --> update visuals.
        if (_containerSystem.TryGetContainingContainer(uid, out var container))
            RaiseLocalEvent(container.Owner, new VisualsChangedEvent(uid, container.ID));
    }

    /// <summary>
    ///     An entity holding this item is requesting visual information for in-hand sprites.
    /// </summary>
    private void OnGetVisuals(EntityUid uid, SharedItemComponent item, GetInhandVisualsEvent args)
    {
        var defaultKey = $"inhand-{args.Location.ToString().ToLowerInvariant()}";

        // try get explicit visuals
        if (item.InhandVisuals == null || !item.InhandVisuals.TryGetValue(args.Location, out var layers))
        {
            // get defaults
            if (!TryGetDefaultVisuals(uid, item, defaultKey,  out layers))
                return;
        }

        var i = 0;
        foreach (var layer in layers)
        {
            var key = layer.MapKeys?.FirstOrDefault();
            if (key == null)
            {
                key = i == 0 ? defaultKey : $"{defaultKey}-{i}";
                i++;
            }

            args.Layers.Add((key, layer));
        }
    }

    /// <summary>
    ///     If no explicit in-hand visuals were specified, this attempts to populate with default values.
    /// </summary>
    /// <remarks>
    ///     Useful for lazily adding in-hand sprites without modifying yaml. And backwards compatibility.
    /// </remarks>
    private bool TryGetDefaultVisuals(EntityUid uid, SharedItemComponent item, string defaultKey, [NotNullWhen(true)] out List<PrototypeLayerData>? result)
    {
        result = null;

        RSI? rsi = null;

        if (item.RsiPath != null)
            rsi = _resCache.GetResource<RSIResource>(TextureRoot / item.RsiPath).RSI;
        else if (TryComp(uid, out SpriteComponent? sprite))
            rsi = sprite.BaseRSI;

        if (rsi == null || rsi.Path == null)
            return false;

        var state = (item.EquippedPrefix == null)
            ? defaultKey
            : $"{item.EquippedPrefix}-{defaultKey}";

        if (!rsi.TryGetState(state, out var _))
            return false;

        var layer = PrototypeLayerData.New();
        layer.RsiPath = rsi.Path.ToString();
        layer.State = state;
        layer.MapKeys = new() { state };

        result = new() { layer };
        return true;
    }
    #endregion
}
