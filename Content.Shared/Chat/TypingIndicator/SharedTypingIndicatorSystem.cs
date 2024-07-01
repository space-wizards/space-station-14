using Content.Shared.ActionBlocker;
using Content.Shared.Clothing;
using Robust.Shared.Player;

namespace Content.Shared.Chat.TypingIndicator;

/// <summary>
///     Supports typing indicators on entities.
/// </summary>
public abstract class SharedTypingIndicatorSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    /// <summary>
    ///     Default ID of <see cref="TypingIndicatorPrototype"/>
    /// </summary>
    [ValidatePrototypeId<TypingIndicatorPrototype>]
    public const string InitialIndicatorId = "default";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<TypingIndicatorComponent, PlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<TypingIndicatorClothingComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<TypingIndicatorClothingComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);

        SubscribeAllEvent<TypingChangedEvent>(OnTypingChanged);
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        // when player poses entity we want to make sure that there is typing indicator
        EnsureComp<TypingIndicatorComponent>(ev.Entity);
        // we also need appearance component to sync visual state
        EnsureComp<AppearanceComponent>(ev.Entity);
    }

    private void OnPlayerDetached(EntityUid uid, TypingIndicatorComponent component, PlayerDetachedEvent args)
    {
        // player left entity body - hide typing indicator
        SetTypingIndicatorEnabled(uid, false);
    }

    private void OnGotEquipped(EntityUid uid, TypingIndicatorClothingComponent component, ClothingGotEquippedEvent args)
    {
        if (!TryComp<TypingIndicatorComponent>(args.Wearer, out var indicator))
            return;

        indicator.Prototype = component.Prototype;
    }

    private void OnGotUnequipped(EntityUid uid, TypingIndicatorClothingComponent component, ClothingGotUnequippedEvent args)
    {
        if (!TryComp<TypingIndicatorComponent>(args.Wearer, out var indicator))
            return;

        indicator.Prototype = InitialIndicatorId;
    }

    private void OnTypingChanged(TypingChangedEvent ev, EntitySessionEventArgs args)
    {
        var uid = args.SenderSession.AttachedEntity;
        if (!Exists(uid))
        {
            Log.Warning($"Client {args.SenderSession} sent TypingChangedEvent without an attached entity.");
            return;
        }

        // check if this entity can speak or emote
        if (!_actionBlocker.CanEmote(uid.Value) && !_actionBlocker.CanSpeak(uid.Value))
        {
            // nah, make sure that typing indicator is disabled
            SetTypingIndicatorEnabled(uid.Value, false);
            return;
        }

        SetTypingIndicatorEnabled(uid.Value, ev.IsTyping);
    }

    private void SetTypingIndicatorEnabled(EntityUid uid, bool isEnabled, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref appearance, false))
            return;

        _appearance.SetData(uid, TypingIndicatorVisuals.IsTyping, isEnabled, appearance);
    }
}
