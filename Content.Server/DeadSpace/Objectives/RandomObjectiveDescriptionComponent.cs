// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

namespace Content.Server.DeadSpace.Objectives;

/// <summary>
/// Хранит пул ключей локали для описания цели.
/// </summary>
[RegisterComponent, Access(typeof(RandomObjectiveDescriptionSystem))]
public sealed partial class RandomObjectiveDescriptionComponent : Component
{
    [DataField]
    public List<LocId> Descriptions = new(); // Заполняемый из YAML список описаний
}
