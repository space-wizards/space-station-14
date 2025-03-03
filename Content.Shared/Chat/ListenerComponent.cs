namespace Content.Shared.Chat;

[RegisterComponent]
public abstract partial class ListenerComponent : Component
{
    /// <summary>
    /// The medium(s) through the message must be delivered to be listened to.
    /// </summary>
    [DataField]
    public ChatChannelMedium? FilteredTypes;
}
