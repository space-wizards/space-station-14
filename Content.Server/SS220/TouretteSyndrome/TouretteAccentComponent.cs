// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Speech.EntitySystems;
namespace Content.Server.Speech.Components;

[RegisterComponent]
[Access(typeof(TouretteAccentSystem))]
public sealed partial class TouretteAccentComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)] public float SwearChance;
    protected internal List<string> TouretteWords;
}
