using Content.Server.DeadSpace.Drug.Components;
using Robust.Shared.Timing;
using Content.Shared.Jittering;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Stunnable;
using Content.Shared.Damage.Components;
using Content.Shared.Popups;
using Content.Server.Temperature.Systems;
using Content.Server.Temperature.Components;
using Content.Shared.Damage;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Content.Shared.Mind.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;

namespace Content.Server.DeadSpace.Drug;

public sealed class DrugAddicationSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedJitteringSystem _sharedJittering = default!;
    [Dependency] private readonly SlurredSystem _slurred = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    public const float MinAddictionLevel = 1;
    public const float MinTolerance = 0.01f;
    public const float DefaultAddictionLevel = 6;
    public const float DefaultTolerance = 0.03f;
    public const float MaxAddictionLevel = 100;
    public const float MaxTolerance = 1;
    public const float MaxWithdrawalLevel = 100;
    public const float MaxAddTemperature = 10;
    public const float MaxWithdrawalRate = 5;
    public const float BaseThresholdTime = 300;
    public const int MaxDrugStr = 4;
    public const bool EnableMaxAddication = false; // настройка зависимости addication от тяжести наркотика (addication не будет превышать значение для зависимости с уровнем тяежсти)
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InstantDrugAddicationComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<InstantDrugAddicationComponent, ComponentShutdown>(OnComponentShut);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<InstantDrugAddicationComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (_gameTiming.CurTime > component.TimeUtilUpdate)
            {
                UpdateDrugAddication(uid, component);
                component.TimeUtilUpdate = _gameTiming.CurTime + component.UpdateDuration;
            }

            if (!component.IsTimeSendMessage && _gameTiming.CurTime > component.TimeUtilSendMessage)
                component.IsTimeSendMessage = true;

        }
    }

    private void OnComponentInit(EntityUid uid, InstantDrugAddicationComponent component, ComponentInit args)
    {
        component.TimeUtilChangeAddiction = _gameTiming.CurTime + component.ChangeAddictionDuration;

        component.Tolerance = DefaultTolerance;
        component.AddictionLevel = DefaultAddictionLevel;
        if (TryComp<TemperatureComponent>(uid, out var temperature))
            component.StandartTemperature = temperature.CurrentTemperature;

        if (TryComp<StaminaComponent>(uid, out var stamina) && component.IsStaminaEdit)
            stamina.CritThreshold /= component.StaminaMultiply;
    }

    private void OnComponentShut(EntityUid uid, InstantDrugAddicationComponent component, ComponentShutdown args)
    {
        RemComp<SlowedDownComponent>(uid);
    }

    public void UpdateDrugAddication(EntityUid uid, InstantDrugAddicationComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (_mobState.IsDead(uid))
            return;

        if (component.AddictionLevel < MinAddictionLevel && component.Tolerance < MinTolerance)
            RemComp<InstantDrugAddicationComponent>(uid);

        var time = component.UpdateDuration;
        float seconds = (float)Math.Abs(time.TotalSeconds);

        AddTimeLastAppointment(uid, seconds, component);

        if (component.TimeLastAppointment > component.SomeThresholdTime)
        {
            // Вычисляем разницу времени с последнего приема
            var timeSinceLastAppointment = component.TimeLastAppointment - component.SomeThresholdTime;

            // Определяем прогрессию WithdrawalRate от 0 до MaxWithdrawalRate
            // Clamp для ограничения значения между 0 и 1
            float progress = Math.Clamp(timeSinceLastAppointment / component.SomeThresholdTime, 0, 1);

            // Вычисляем новый WithdrawalRate с интерполяцией
            component.WithdrawalRate = progress * MaxWithdrawalRate;

            UpdateMaxWithdrawalLevel(uid, component);
            UpdateWithdrawalLevel(uid, component);
            RunEffects(uid, component);
        }

        if (_gameTiming.CurTime > component.TimeUtilChangeAddiction)
        {
            component.AddictionLevel = Math.Max(0, component.AddictionLevel - 0.5f);
            component.Tolerance = Math.Max(0, component.Tolerance - 0.01f);
            component.TimeUtilChangeAddiction = _gameTiming.CurTime + component.ChangeAddictionDuration;
        }

    }

    public void AddAddictionLevel(EntityUid uid, float effectStrenght, InstantDrugAddicationComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.AddictionLevel = Math.Min(MaxAddictionLevel, component.AddictionLevel + effectStrenght * (1 - component.Tolerance));

        if (EnableMaxAddication)
            component.AddictionLevel = Math.Min(MaxAddictionLevel * component.DependencyLevel / MaxDrugStr, component.AddictionLevel);
    }

    public void AddTolerance(EntityUid uid, float effectStrenght, InstantDrugAddicationComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Tolerance = Math.Min(MaxTolerance, component.Tolerance + effectStrenght);
    }

    public void TakeDrug(EntityUid uid, int drugStrenght, float addictionStrenght, float toleranceStrenght, InstantDrugAddicationComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        drugStrenght = Math.Min(MaxDrugStr, drugStrenght);

        if (component.DependencyLevel <= drugStrenght)
        {
            AddAddictionLevel(uid, addictionStrenght, component);
            AddTolerance(uid, toleranceStrenght, component);
            component.DependencyLevel = drugStrenght;
            component.TimeLastAppointment = 0;
            CalculateThresholdTime(component);
        }
        else if (!component.IsTakeWeakDrug && _gameTiming.CurTime > component.DurationOfActionWeakDrug)
        {
            var strenght = drugStrenght / MaxDrugStr;

            AddAddictionLevel(uid, addictionStrenght * strenght, component);
            AddTolerance(uid, toleranceStrenght * strenght, component);
            float randomFactor = Random.Shared.Next(100, 300) * strenght;
            AddTimeLastAppointment(uid, -randomFactor, component);
            component.DurationOfActionWeakDrug = _gameTiming.CurTime + TimeSpan.FromSeconds(randomFactor);
        }
    }

    public void AddTimeLastAppointment(EntityUid uid, float count, InstantDrugAddicationComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.TimeLastAppointment += count;
        component.TimeLastAppointment = Math.Max(0, component.TimeLastAppointment);
    }

    public void UpdateWithdrawalLevel(EntityUid uid, InstantDrugAddicationComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.WithdrawalLevel = Math.Min(MaxWithdrawalLevel, component.WithdrawalLevel + component.WithdrawalRate);
        component.WithdrawalLevel = Math.Min(component.MaxWithdrawalLvl, component.WithdrawalLevel);
    }

    public void UpdateMaxWithdrawalLevel(EntityUid uid, InstantDrugAddicationComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        // Влияние зависимости: чем выше зависимость, тем выше MaxWithdrawalLvl
        float addictionImpact = component.AddictionLevel / MaxAddictionLevel;

        // Влияние толерантности: чем выше толерантность, тем меньше MaxWithdrawalLvl
        float toleranceImpact = 1 - (component.Tolerance / MaxTolerance);

        // Общий расчет MaxWithdrawalLvl
        component.MaxWithdrawalLvl = Math.Min(
            MaxWithdrawalLevel,
            MaxWithdrawalLevel * addictionImpact * toleranceImpact);
    }

    public void RunEffects(EntityUid uid, InstantDrugAddicationComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.WithdrawalLevel >= 10 && component.WithdrawalLevel < 25)
        {
            if (component.IsTimeSendMessage)
            {
                _popup.PopupEntity(Loc.GetString("drug-addication-effects-low"), uid, uid);
                component.TimeUtilSendMessage = _gameTiming.CurTime + component.SendMessageDuration;
                component.IsTimeSendMessage = false;
            }

            LowEffects(uid, component);
        } else if (component.WithdrawalLevel >= 25 && component.WithdrawalLevel < 50)
        {
            if (component.IsTimeSendMessage)
            {
                _popup.PopupEntity(Loc.GetString("drug-addication-effects-medium"), uid, uid);
                component.TimeUtilSendMessage = _gameTiming.CurTime + component.SendMessageDuration;
                component.IsTimeSendMessage = false;
            }

            MediumEffects(uid, component);
        } else if (component.WithdrawalLevel >= 50 && component.WithdrawalLevel < 75)
        {
            if (component.IsTimeSendMessage)
            {
                _popup.PopupEntity(Loc.GetString("drug-addication-effects-medium-plus"), uid, uid);
                component.TimeUtilSendMessage = _gameTiming.CurTime + component.SendMessageDuration;
                component.IsTimeSendMessage = false;
            }

            MediumPlusEffects(uid, component);
        } else if (component.WithdrawalLevel >= 75)
        {
            if (component.IsTimeSendMessage)
            {
                _popup.PopupEntity(Loc.GetString("drug-addication-effects-high"), uid, uid);
                component.TimeUtilSendMessage = _gameTiming.CurTime + component.SendMessageDuration;
                component.IsTimeSendMessage = false;
            }

            HighEffects(uid, component);
        }
        else
        {
            RemComp<SlowedDownComponent>(uid);

            if (TryComp<StaminaComponent>(uid, out var stamina) && !component.IsStaminaEdit)
            {
                stamina.CritThreshold /= component.StaminaMultiply;
                component.IsStaminaEdit = false;
            }
        }
    }

    private void HighEffects(EntityUid uid, InstantDrugAddicationComponent component)
    {
        MediumPlusEffects(uid, component);
        _slurred.DoSlur(uid, TimeSpan.FromSeconds(1f) + component.UpdateDuration);
        EnsureComp<SlowedDownComponent>(uid);
        _damageable.TryChangeDamage(uid, component.Damage, true);

        if (TryComp<MindContainerComponent>(uid, out var mind)
        && TryComp<MindComponent>(mind.Mind, out var mindComp) && mindComp.Session != null)
        {
            Filter playerFilter = Filter.Empty().AddPlayer(mindComp.Session);
            _audio.PlayGlobal(component.SoundHighEffect, playerFilter, false);
        }
    }

    private void MediumPlusEffects(EntityUid uid, InstantDrugAddicationComponent component)
    {
        _sharedJittering.DoJitter(uid, component.UpdateDuration, true, 10f * component.EffectStrengthModify);
        MediumEffects(uid, component);

        if (!TryComp<TemperatureComponent>(uid, out var temperature))
            return;

        if (temperature.CurrentTemperature > MaxAddTemperature + component.StandartTemperature)
            return;

        _temperature.ChangeHeat(uid, 4000 * component.EffectStrengthModify, true, temperature);
    }

    private void MediumEffects(EntityUid uid, InstantDrugAddicationComponent component)
    {
        LowEffects(uid, component);

        if (!TryComp<TemperatureComponent>(uid, out var temperature))
            return;

        if (temperature.CurrentTemperature > MaxAddTemperature + component.StandartTemperature)
            return;

        _temperature.ChangeHeat(uid, 4000 * component.EffectStrengthModify, true, temperature);
    }

    private void LowEffects(EntityUid uid, InstantDrugAddicationComponent component)
    {
        if (TryComp<StaminaComponent>(uid, out var stamina) && !component.IsStaminaEdit)
        {
            stamina.CritThreshold *= component.StaminaMultiply;
            component.IsStaminaEdit = true;
        }
    }

    public float CalculateThresholdTime(InstantDrugAddicationComponent component)
    {
        // Рассчитываем модификатор зависимости: чем выше зависимость, тем меньше thresholdTime
        float addictionModifier = 1 - (component.AddictionLevel / MaxAddictionLevel);

        // Рассчитываем модификатор толерантности: чем выше толерантность, тем больше времени до ломки
        float toleranceModifier = 1 + (component.Tolerance / MaxTolerance);

        // Генерируем случайное значение в диапазоне от -10 до +10 секунд для случайности
        float randomFactor = Random.Shared.Next(-10, 11);

        // Вычисляем итоговое значение времени
        float thresholdTime = BaseThresholdTime * addictionModifier * toleranceModifier + randomFactor;

        // Ограничиваем значение, чтобы не было слишком короткого или слишком длинного времени
        component.SomeThresholdTime = Math.Clamp(thresholdTime, 300, 600);

        return component.SomeThresholdTime; // от 5 минут до 10 минут
    }
}
