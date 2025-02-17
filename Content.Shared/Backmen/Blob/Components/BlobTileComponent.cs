using Content.Shared.Backmen.Blob;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Backmen.Blob.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class BlobTileComponent : Component
{
    [DataField("color"), AutoNetworkedField]
    public Color Color = Color.White;

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid? Core = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool ReturnCost = true;

    [ViewVariables(VVAccess.ReadOnly), DataField("tileType")]
    public BlobTileType BlobTileType = BlobTileType.Normal;

    [ViewVariables(VVAccess.ReadOnly), DataField("healthOfPulse")]
    public DamageSpecifier HealthOfPulse = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Blunt", -4 },
            { "Slash", -4 },
            { "Piercing", -4 },
            { "Heat", -4 },
            { "Cold", -4 },
            { "Shock", -4 },
        }
    };

    [ViewVariables(VVAccess.ReadOnly), DataField("flashDamage")]
    public DamageSpecifier FlashDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Heat", 100 },
        }
    };
}

[Serializable]
public enum BlobTileType : byte
{
    Normal,
    Strong,
    Reflective,
    Resource,
    Storage,
    Node,
    Factory,
    Core,
    None,
}
