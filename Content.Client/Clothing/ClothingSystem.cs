using System.Collections.Generic;
using Content.Shared.Clothing;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;

namespace Content.Client.Clothing;

public class ClothingSystem : EntitySystem
{
    /// <summary>
    /// This is a shitty hotfix written by me (Paul) to save me from renaming all files.
    /// For some context, im currently refactoring inventory. Part of that is slots not being indexed by a massive enum anymore, but by strings.
    /// Problem here: Every rsi-state is using the old enum-names in their state. I already used the new inventoryslots ALOT. tldr: its this or another week of renaming files.
    /// </summary>
    private static readonly Dictionary<string, string> TemporarySuffixMap = new()
    {
        {"head", "HEAD"},
        {"eyes", "EYES"},
        {"ears", "EARS"},
        {"mask", "MASK"},
        {"outerClothing", "OUTERCLOTHING"},
        {"jumpsuit", "INNERCLOTHING"},
        {"neck", "NECK"},
        {"back", "BACKPACK"},
        {"belt", "BELT"},
        {"gloves", "GLOVES"},
        {"shoes", "SHOES"},
        {"id", "IDCARD"},
        {"pocket1", "POCKET1"},
        {"pocket2", "POCKET2"},
    };

    [Dependency] private IResourceCache _cache = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingComponent, ComponentHandleState>(OnComponentHandleState);
    }

    private void OnComponentHandleState(EntityUid uid, ClothingComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ClothingComponentState state)
        {
            return;
        }

        component.ClothingEquippedPrefix = state.ClothingEquippedPrefix;
    }

    public (RSI rsi, RSI.StateId stateId)? GetEquippedStateInfo(EntityUid uid, string slot, string? speciesId=null, ClothingComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return null;

        if (component.RsiPath == null)
            return null;

        var rsi = _cache.GetResource<RSIResource>(SharedSpriteComponent.TextureRoot / component.RsiPath).RSI;
        var prefix = component.ClothingEquippedPrefix ?? component.EquippedPrefix;
        if (prefix != null)
            TemporarySuffixMap.TryGetValue(prefix, out prefix);
        var stateId = prefix != null ? $"{prefix}-equipped-{slot}" : $"equipped-{slot}";
        if (speciesId != null)
        {
            var speciesState = $"{stateId}-{speciesId}";
            if (rsi.TryGetState(speciesState, out _))
            {
                return (rsi, speciesState);
            }
        }

        if (rsi.TryGetState(stateId, out _))
        {
            return (rsi, stateId);
        }

        return null;
    }
}
