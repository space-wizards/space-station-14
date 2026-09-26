using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.AttachedVisuals;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Client.Items.Systems;

public sealed partial class ItemSystem : SharedItemSystem
{
    [Dependency] private IResourceCache _resCache = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        //SubscribeLocalEvent<ItemComponent, GetInhandVisualsEvent>(OnGetVisuals);

        // TODO is this still needed? Shouldn't containers occlude them?
        //SubscribeLocalEvent<SpriteComponent, GotEquippedEvent>(OnEquipped);
        //SubscribeLocalEvent<SpriteComponent, GotUnequippedEvent>(OnUnequipped);
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<ItemComponent> ent, ref ComponentStartup args)
    {
        var attachedVisuals = EnsureComp<AttachedVisualsComponent>(ent);

        if (attachedVisuals.RsiPath == null)
            attachedVisuals.RsiPath = ent.Comp.RsiPath;

        if (ent.Comp.InhandVisuals.Count == 0
            && !attachedVisuals.AttachedVisuals.ContainsKey("InhandLeft")
            && TryGetDefaultVisuals(ent, ent.Comp, "inhand-left", out var leftLayerData))
        {
            attachedVisuals.AttachedVisuals.GetOrNew("InhandLeft").Layers = leftLayerData;
        }

        if (ent.Comp.InhandVisuals.Count == 0
            && !attachedVisuals.AttachedVisuals.ContainsKey("InhandRight")
            && TryGetDefaultVisuals(ent, ent.Comp, "inhand-right", out var rightLayerData))
        {
            attachedVisuals.AttachedVisuals.GetOrNew("InhandRight").Layers = rightLayerData;
        }

        if (!attachedVisuals.AttachedVisuals.ContainsKey("InhandLeft")
            && ent.Comp.InhandVisuals.TryGetValue(HandLocation.Left, out var leftInhandVisuals))
        {
            attachedVisuals.AttachedVisuals.GetOrNew("InhandLeft").Layers = leftInhandVisuals;
        }

        if (!attachedVisuals.AttachedVisuals.ContainsKey("InhandRight")
            && ent.Comp.InhandVisuals.TryGetValue(HandLocation.Right, out var rightInhandVisuals))
        {
            attachedVisuals.AttachedVisuals.GetOrNew("InhandRight").Layers = rightInhandVisuals;
        }
    }

    private void OnUnequipped(EntityUid uid, SpriteComponent component, GotUnequippedEvent args)
    {
        _sprite.SetVisible((uid, component), true);
    }

    private void OnEquipped(EntityUid uid, SpriteComponent component, GotEquippedEvent args)
    {
        _sprite.SetVisible((uid, component), false);
    }

    #region InhandVisuals

    /// <summary>
    ///     When an items visual state changes, notify and entities that are holding this item that their sprite may need updating.
    /// </summary>
    public override void VisualsChanged(EntityUid uid)
    {
        // if the item is in a container, it might be equipped to hands or inventory slots --> update visuals.
        if (Container.TryGetContainingContainer((uid, null, null), out var container))
            RaiseLocalEvent(container.Owner, new VisualsChangedEvent(GetNetEntity(uid), container.ID));
    }

    /// <summary>
    ///     An entity holding this item is requesting visual information for in-hand sprites.
    /// </summary>
    private void OnGetVisuals(EntityUid uid, ItemComponent item, GetInhandVisualsEvent args)
    {
        var defaultKey = $"inhand-{args.Location.ToString().ToLowerInvariant()}";

        // try get explicit visuals
        if (!item.InhandVisuals.TryGetValue(args.Location, out var layers))
        {
            // get defaults
            if (!TryGetDefaultVisuals(uid, item, defaultKey, out layers))
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
    private bool TryGetDefaultVisuals(EntityUid uid, ItemComponent item, string defaultKey, [NotNullWhen(true)] out List<PrototypeLayerData>? result)
    {
        result = null;

        RSI? rsi = null;

        if (item.RsiPath != null)
            rsi = _resCache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / item.RsiPath).RSI;
        else if (TryComp(uid, out SpriteComponent? sprite))
            rsi = sprite.BaseRSI;

        if (rsi == null)
            return false;

        var state = (item.HeldPrefix == null)
            ? defaultKey
            : $"{item.HeldPrefix}-{defaultKey}";

        if (!rsi.TryGetState(state, out var _))
            return false;

        var layer = new PrototypeLayerData();
        layer.RsiPath = rsi.Path.ToString();
        layer.State = state;
        layer.MapKeys = new() { state };

        result = new() { layer };
        return true;
    }
    #endregion
}
