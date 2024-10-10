using System.Collections.Frozen;
using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.EntityEffects.Effects;
using Content.Server.Popups;
using Content.Server.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Actions;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Emoting;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Inventory;
using Content.Shared.Speech;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Speech;

public sealed class EmoteBlockerSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    // Cache the Scream Emote's prototype so that it can be used when we deal with the Scream Action.
    private EmotePrototype? _screamPrototype = null;

    public override void Initialize()
    {
        base.Initialize();

        // Intercept SceamActionEvent specifically because it's kinda like an emote, but isn't handled by the blocking code for emotes.
        SubscribeLocalEvent<ScreamActionEvent>(OnScreamAction, before: [typeof(VocalSystem)]);
        SubscribeLocalEvent<EmoteBlockerComponent, GetEmoteBlockersEvent>(OnGetEmoteBlockers);
        SubscribeLocalEvent<EmoteBlockerComponent, InventoryRelayedEvent<GetEmoteBlockersEvent>>(OnRelayedGetEmoteBlockers);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypeReload);
    }

    private void OnScreamAction(ScreamActionEvent args)
    {
        if (args.Handled || _screamPrototype == null)
        {
            return;
        }

        var ev = new GetEmoteBlockersEvent();
        RaiseLocalEvent(args.Performer, ref ev);

        // Handle ScreamActionEvent like it's a Scream emote.
        if (ev.ShouldBlock(_screamPrototype))
        {
            _popupSystem.PopupEntity(Loc.GetString("emote-blocked", ("emote", Loc.GetString(_screamPrototype.Name).ToLower())), args.Performer, args.Performer);
            args.Handled = true;
        }
    }

    private void OnRelayedGetEmoteBlockers(EntityUid uid, EmoteBlockerComponent component, InventoryRelayedEvent<GetEmoteBlockersEvent> args)
    {
        OnGetEmoteBlockers(uid, component, args.Args);
    }

    private void OnGetEmoteBlockers(EntityUid uid, EmoteBlockerComponent component, GetEmoteBlockersEvent args)
    {
        foreach (var category in component.BlocksCategories)
        {
            args.BlockedCategories.Add(category);
        }
        foreach (var emote in component.BlocksEmotes)
        {
            args.BlockedEmotes.Add(emote);
        }
    }

    private void OnPrototypeReload(PrototypesReloadedEventArgs obj)
    {
        if (obj.WasModified<EmotePrototype>())
        {
            _screamPrototype = null;
            foreach (var emote in _prototypeManager.EnumeratePrototypes<EmotePrototype>())
            {
                if (emote.ID == "Scream")
                {
                    _screamPrototype = emote;
                    break;
                }
            }
        }
    }
}
