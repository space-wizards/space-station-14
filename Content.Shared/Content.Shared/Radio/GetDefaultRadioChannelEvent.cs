using Content.Shared.Chat;
using Content.Shared.Inventory;

namespace Content.Shared.Radio;

public sealed class GetDefaultRadioChannelEvent : EntityEventArgs, IInventoryRelayEvent
{
    /// <summary>
    ///     Id of the default <see cref="RadioChannelPrototype"/> that will get addressed when using the
    ///     department/default channel prefix. See <see cref="SharedChatSystem.DefaultChannelKey"/>.
    /// </summary>
    public string? Channel;

    public SlotFlags TargetSlots => ~SlotFlags.POCKET;
}
