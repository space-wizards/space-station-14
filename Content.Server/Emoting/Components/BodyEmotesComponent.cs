using Content.Shared.Chat.Prototypes;

namespace Content.Server.Emoting.Components;

[RegisterComponent]
public sealed class BodyEmotesComponent : Component
{
    [DataField("soundsId")]
    public string? SoundsId;

    public EmoteSoundsPrototype? Sounds;
}
