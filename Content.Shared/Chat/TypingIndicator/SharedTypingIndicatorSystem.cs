using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;

namespace Content.Shared.Chat.TypingIndicator;

/// <summary>
///     Sync typing indicator icon between client and server.
/// </summary>
public abstract class SharedTypingIndicatorSystem : EntitySystem
{
    /// <summary>
    ///     Default ID of <see cref="TypingIndicatorPrototype"/>
    /// </summary>
    public const string InitialIndicatorId = "default";
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TypingIndicatorClothingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<TypingIndicatorClothingComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(EntityUid uid, TypingIndicatorClothingComponent component, GotEquippedEvent args)
    {
        if (!TryComp(uid, out ClothingComponent? clothing))
            return;
        
        var isCorrectSlot = clothing.Slots.HasFlag(args.SlotFlags);
        if (!isCorrectSlot) return;
        
        if (!TryComp<TypingIndicatorComponent>(args.Equipee, out var indicator))
            return;

        indicator.Prototype = component.Prototype;
    }

    private void OnGotUnequipped(EntityUid uid, TypingIndicatorClothingComponent component, GotUnequippedEvent args)
    {
        if (!TryComp<TypingIndicatorComponent>(args.Equipee, out var indicator))
            return;

        indicator.Prototype = SharedTypingIndicatorSystem.InitialIndicatorId;
    }
}
