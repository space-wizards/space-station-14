
namespace Content.Server.Magic;

[RegisterComponent]
public sealed partial class ChainFireballComponent : Component
{
    public float Divisions = 0f;
    [DataField] public float MaxDivisions = 4f;
}
