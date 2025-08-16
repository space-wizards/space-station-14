namespace Content.Shared.Silicons.Bots;

/// <summary>
/// This component describes how a HugBot hugs.
/// </summary>
/// <see cref="SharedHugBotSystem"/>
[RegisterComponent, AutoGenerateComponentState]
public sealed partial class HugBotComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan HugCooldown = TimeSpan.FromMinutes(2);
}
