using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client.Items.Systems;

public sealed class ItemSystem : SharedItemSystem
{
    [Dependency] private readonly IResourceCache _resCache = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemComponent, GetInhandVisualsEvent>(OnGetVisuals);

        // TODO is this still needed? Shouldn't containers occlude them?
        SubscribeLocalEvent<SpriteComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<SpriteComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnUnequipped(Entity<SpriteComponent> ent, ref GotUnequippedEvent args)
    {
        _sprite.SetVisible((ent, ent.Comp), true);
    }

    private void OnEquipped(Entity<SpriteComponent> ent, ref GotEquippedEvent args)
    {
        _sprite.SetVisible((ent, ent.Comp), false);
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
    private void OnGetVisuals(Entity<ItemComponent> ent, ref GetInhandVisualsEvent args)
    {
        var defaultKey = $"inhand-{args.Location.ToString().ToLowerInvariant()}";

        // try get explicit visuals
        if (!ent.Comp.InhandVisuals.TryGetValue(args.Location, out var layers))
        {
            // get defaults
            if (!TryGetDefaultVisuals(ent, defaultKey, out layers))
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
    private bool TryGetDefaultVisuals(Entity<ItemComponent> ent, string defaultKey, [NotNullWhen(true)] out List<PrototypeLayerData>? result)
    {
        result = null;

        RSI? rsi = null;

        if (ent.Comp.RsiPath != null)
            rsi = _resCache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / ent.Comp.RsiPath).RSI;
        else if (TryComp(ent, out SpriteComponent? sprite))
            rsi = sprite.BaseRSI;

        if (rsi == null)
            return false;

        var state = (ent.Comp.HeldPrefix == null)
            ? defaultKey
            : $"{ent.Comp.HeldPrefix}-{defaultKey}";

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
