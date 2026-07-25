// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.Hooligan.Objectives;

/// <summary>
/// Компонент Разрушения для Хулигана
/// Хранит количество и тэг прототипа
/// </summary>
[RegisterComponent, Access(typeof(HooliganDestroyConditionSystem))]
public sealed partial class HooliganDestroyConditionComponent : Component
{
    public int Count; // Сколько сломано

    [DataField]
    public List<ProtoId<TagPrototype>> Targets = new(); // Тэг для прототипов по цели
}
