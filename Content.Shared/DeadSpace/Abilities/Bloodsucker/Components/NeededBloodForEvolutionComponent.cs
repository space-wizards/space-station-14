// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Abilities.Bloodsucker.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class NeededBloodForEvolutionComponent : Component
{
    [DataField("defaultCost")]
    public float DefaultCost = 50f;

    // Словарь для хранения соответствия элементов SpawnedEntities и цен крови
    [DataField("bloodCosts")]
    public Dictionary<string, float> BloodCosts { get; set; } = new Dictionary<string, float>();
}
