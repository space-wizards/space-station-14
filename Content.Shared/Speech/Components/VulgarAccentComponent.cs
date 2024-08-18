using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class VulgarAccentComponent : Component
{
    [DataField]
    public string[] SwearWords =
    {
        "FUCK",
        "FUCKING HELL",
        "GOD DAMN",
        "SHIT",
        "BIG SACK OF SHIT",
        "SON OF A BITCH",
        "BITCH",
        "COCK",
        "COCKSUCKER",
        "BONER SHIT COCKS",
        "MOTHERFUCKER",
        "YOU BASTARD",
        "DICK",
        "DICKBAG",
        "ASSHOLE",
        "SHITTY ASS FUCKHOLE",
        "GOOD HEAVENS",
        "SON OF A CLUWNE"
    };

    [DataField]
    public float SwearProb = 0.5f;

}
