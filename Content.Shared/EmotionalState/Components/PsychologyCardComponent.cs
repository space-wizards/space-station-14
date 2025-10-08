using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Content.Shared.EmotionalState;

[RegisterComponent, NetworkedComponent, Access(typeof(EmotionalStateSystem))]
public sealed partial class PsychologyCardComponent : Component
{
    /// <summary>
    /// List of trigger prototypes. Needed to group certain items into a single psychological trigger group.
    /// For example, potted plants or trees. That is, a medium-sized tree might provide more positive
    /// emotions than a small tree, but they both have a beneficial effect anyway. Actually, this is
    /// a workaround to avoid creating 100+ cards.
    /// </summary>
    [DataField("triggers"), ViewVariables(VVAccess.ReadWrite)]
    public List<string> Triggers = new List<string>();

    /// <summary>
    /// Localization string that will be displayed when viewing the object
    /// </summary>
    [DataField("locName"), ViewVariables(VVAccess.ReadWrite)]
    public string LocName = "";

    /// <summary>
    /// The number of points the humanoid will gain/lose upon seeing the card.
    /// </summary>
    [DataField("emotionalEffect"), ViewVariables(VVAccess.ReadWrite)]
    public float EmotionalEffect = 0f;
}
