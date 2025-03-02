namespace Content.Shared.Chat;

[RegisterComponent]
public abstract partial class ListenerComponent : Component
{
    [DataField]
    public ChatChannelMedium? FilteredTypes;
}
