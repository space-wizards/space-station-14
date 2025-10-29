// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Alert;
using Content.Shared.DeadSpace.Abilities.Bloodsucker.Components;
using Content.Shared.Whitelist;

namespace Content.Shared.DeadSpace.Abilities.Bloodsucker;

public abstract class SharedBloodsuckerSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public float CalculateBloodCost(string selectEntity, Dictionary<string, float> bloodCosts, float defaultCost)
    {
        // Проверяем, есть ли SelectEntity в словаре BloodCosts
        if (bloodCosts.ContainsKey(selectEntity))
        {
            // Если совпадение найдено, вычитаем соответствующую цену крови
            return bloodCosts[selectEntity];
        }
        else
        {
            // Если совпадение не найдено, возвращаем значение по умолчанию
            return defaultCost;
        }
    }

    public float AddReagentCount(EntityUid uid, float count, BloodsuckerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return count;

        var bloodsuckEvent = new BloodsuckEvent(count);
        RaiseLocalEvent(uid, bloodsuckEvent);

        if (bloodsuckEvent.Handled)
        {
            UpdateBloodAlert(uid, component);
            return component.CountReagent;
        }

        component.CountReagent += count;
        UpdateBloodAlert(uid, component);
        return component.CountReagent;
    }

    public void SetReagentCount(EntityUid uid, float newQuantity, BloodsuckerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.CountReagent = newQuantity;
        UpdateBloodAlert(uid, component);
    }

    public void UpdateBloodAlert(EntityUid ent, BloodsuckerComponent? component = null)
    {
        if (!Resolve(ent, ref component))
            return;

        float bloodPercentage = (component.CountReagent / component.MaxCountReagent) * 100;

        // Преобразовать процент крови в уровень важности
        short severity = (short)(bloodPercentage / 5);

        // Убедиться, что уровень важности находится в допустимом диапазоне
        if (severity <= 0) severity = 0;
        if (severity >= 20) severity = 20;

        // Вызвать ShowAlert
        _alerts.ClearAlert(ent, component.BloodAlert);
        _alerts.ShowAlert(ent, component.BloodAlert, severity);
    }
}
