using Content.Client.Actions;
using Content.Client.UserInterface.Controls;
using Content.Shared.Actions.Components;
using Content.Shared.CF.Actions;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Speech;
using Content.Shared.Whitelist;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.CF.Hud;

/// <summary>
/// The wheel: vocal chat, actions, free actions, cantrips, and spell ranks.
/// Ranks 1 through 3 are always present. A higher rank appears when an action uses it.
/// </summary>
public static class CFRadialMenu
{
    private const int AlwaysShownRanks = 4;

    private static readonly Color ActionColor = new(46, 120, 52, 190);
    private static readonly Color ActionHot = new(96, 176, 88, 220);
    private static readonly Color FreeColor = new(22, 22, 24, 210);
    private static readonly Color FreeHot = new(78, 78, 84, 230);
    private static readonly Color CantripColor = new(64, 48, 120, 200);
    private static readonly Color CantripHot = new(140, 110, 210, 230);
    private static readonly Color SpellColor = new(36, 72, 140, 190);
    private static readonly Color SpellHot = new(90, 150, 210, 220);
    private static readonly Color BackColor = new(48, 48, 54, 200);
    private static readonly Color BackHot = new(90, 90, 98, 230);

    public static List<RadialMenuOptionBase> Build(
        SimpleRadialMenu menu,
        IEnumerable<EmotePrototype> emotes,
        EntityUid? player,
        EntityWhitelistSystem whitelist,
        IEntityManager entities,
        ActionsSystem actions,
        Action<EmotePrototype> playEmote,
        Action<Entity<ActionComponent>> useAction)
    {
        var vocal = Layer(menu);
        foreach (var emote in emotes)
        {
            if (emote.Category != EmoteCategory.Vocal || emote.ChatTriggers.Count == 0)
                continue;

            if (player is not { } body)
                continue;

            if (!whitelist.IsWhitelistPassOrNull(emote.Whitelist, body)
                || whitelist.IsWhitelistPass(emote.Blacklist, body))
                continue;

            if (!emote.Available
                && entities.TryGetComponent<SpeechComponent>(body, out var speech)
                && !speech.AllowedEmotes.Contains(emote.ID))
                continue;

            vocal.Add(new RadialMenuActionOption<EmotePrototype>(playEmote, emote)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(emote.Icon),
                ToolTip = Loc.GetString(emote.Name),
            });
        }

        var main = Layer(menu);
        var free = Layer(menu);
        var rankCount = AlwaysShownRanks;
        foreach (var action in actions.GetClientActions())
        {
            if (!entities.TryGetComponent<CFRadialSlotComponent>(action.Owner, out var slot))
                continue;

            if (slot.Group == CFRadialGroup.Spell && slot.SpellRank + 1 > rankCount)
                rankCount = Math.Min(slot.SpellRank + 1, 11);
        }

        var spells = new List<RadialMenuOptionBase>[rankCount];
        for (var rank = 0; rank < spells.Length; rank++)
            spells[rank] = Layer(menu);

        foreach (var action in actions.GetClientActions())
        {
            if (!entities.TryGetComponent<CFRadialSlotComponent>(action.Owner, out var slot)
                || slot.Group == CFRadialGroup.Reaction)
                continue;

            var option = new RadialMenuActionOption<Entity<ActionComponent>>(useAction, action)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(action.Owner),
                ToolTip = entities.GetComponent<MetaDataComponent>(action.Owner).EntityName,
            };

            switch (slot.Group)
            {
                case CFRadialGroup.Action:
                    main.Add(option);
                    break;
                case CFRadialGroup.Free:
                    free.Add(option);
                    break;
                case CFRadialGroup.Spell when slot.SpellRank >= 0 && slot.SpellRank < spells.Length:
                    spells[slot.SpellRank].Add(option);
                    break;
            }
        }

        var wheel = new List<RadialMenuOptionBase>
        {
            Nest(vocal, 0, "cf-radial-vocal",
                new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/Emotes/vocal.png"))),
            Nest(main, 1, "cf-radial-actions", ActionColor, ActionHot),
            Nest(free, 2, "cf-radial-free", FreeColor, FreeHot),
            Nest(spells[0], 3, "cf-radial-cantrips", CantripColor, CantripHot),
        };

        for (var rank = 1; rank < spells.Length; rank++)
        {
            var label = Loc.GetString("cf-radial-rank", ("rank", rank));
            wheel.Add(Nest(spells[rank], 3 + rank, label, SpellColor, SpellHot, rank.ToString()));
        }

        return wheel;
    }

    private static List<RadialMenuOptionBase> Layer(SimpleRadialMenu menu)
    {
        return new List<RadialMenuOptionBase>
        {
            new RadialMenuActionOption<bool>(_ => menu.ReturnToPreviousLayer(), true)
            {
                Text = Loc.GetString("cf-radial-back"),
                ToolTip = Loc.GetString("cf-radial-back"),
                Order = -1,
                CloseOnPressed = false,
                BackgroundColor = BackColor,
                HoverBackgroundColor = BackHot,
            },
        };
    }

    private static RadialMenuNestedLayerOption Nest(
        List<RadialMenuOptionBase> entries,
        int order,
        string labelOrLoc,
        Color background,
        Color hover,
        string? text = null)
    {
        var label = labelOrLoc.StartsWith("cf-") ? Loc.GetString(labelOrLoc) : labelOrLoc;
        return new RadialMenuNestedLayerOption(entries, 140)
        {
            Order = order,
            Text = text ?? label,
            ToolTip = label,
            BackgroundColor = background,
            HoverBackgroundColor = hover,
        };
    }

    private static RadialMenuNestedLayerOption Nest(
        List<RadialMenuOptionBase> entries,
        int order,
        string locId,
        SpriteSpecifier icon)
    {
        var label = Loc.GetString(locId);
        return new RadialMenuNestedLayerOption(entries, 140)
        {
            Order = order,
            ToolTip = label,
            IconSpecifier = RadialMenuIconSpecifier.With(icon),
        };
    }
}
