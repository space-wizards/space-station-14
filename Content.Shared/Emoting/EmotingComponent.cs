using Content.Shared.Chat;
using Robust.Shared.GameStates;

namespace Content.Shared.Emoting;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmotingComponent : Component
{
    [DataField, AutoNetworkedField]
    [Access(typeof(EmoteSystem), Friend = AccessPermissions.ReadWrite, Other = AccessPermissions.Read)]
    public bool Enabled = true;

    // SS220 Chat-Emote-Cooldown begin
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ChatEmoteCooldown = TimeSpan.FromSeconds(0.5);

    [ViewVariables]
    [Access(typeof(SharedChatSystem), Friend = AccessPermissions.ReadWrite, Other = AccessPermissions.Read)]
    public TimeSpan? LastChatEmoteTime;
    // SS220 Chat-Emote-Cooldown end
}
