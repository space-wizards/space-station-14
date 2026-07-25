// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using Content.Server.Guardian;
using Content.Shared.DeadSpace.Virus.Components;
using Content.Shared.DeadSpace.Virus.Symptoms;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Shared.DeadSpace.TimeWindow;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.DeadSpace.Virus;
using Content.Shared.Whitelist;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Server.DeadSpace.Virus.Symptoms;
using Content.Shared.Tag;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Mobs;
using Content.Server.DeadSpace.Virus.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.DeadSpace.Virus.Prototypes;
using Content.Shared.Body.Prototypes;
using Content.Shared.Mobs.Systems;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Movement.Systems;

namespace Content.Server.DeadSpace.Virus.Systems;

public sealed partial class VirusSystem : SharedVirusSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly TimedWindowSystem _timedWindowSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    private ISawmill _sawmill = default!;

    /// <summary>
    ///     Метка для сущностей, которые инфецируются со 100% вероятностью.
    /// </summary>
    public readonly ProtoId<TagPrototype> VirusAlwaysInfectableTag = "VirusAlwaysInfectable";

    /// <summary>
    ///     Метка для сущностей, которые игнорируют проверку возможности заражения.
    /// </summary>
    public readonly ProtoId<TagPrototype> IgnoreCanInfectTag = "IgnoreCanInfect";

    /// <summary>
    ///     Во время EntityQueryEnumerator может произойти изменение query из-за обновления симптома.
    ///     Поэтому требуется обновлять в списке.
    /// </summary>
    private readonly List<EntityUid> _virusUpdateQueue = new();
    private readonly HashSet<Entity<MobStateComponent>> _nearbyInfectionTargets = new();
    private readonly Dictionary<string, int> _infectedByStrain = new(StringComparer.Ordinal);
    private bool _infectionLookupInProgress;

    public const SlotFlags ProtectiveSlots =
            SlotFlags.FEET |
            SlotFlags.HEAD |
            SlotFlags.EYES |
            SlotFlags.GLOVES |
            SlotFlags.MASK |
            SlotFlags.NECK |
            SlotFlags.INNERCLOTHING |
            SlotFlags.OUTERCLOTHING;
    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("VirusSystem");

        SubscribeLocalEvent<VirusComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<VirusComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<VirusComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<VirusComponent, CauseVirusEvent>(OnCauseVirus);
        SubscribeLocalEvent<VirusComponent, CureVirusEvent>(OnCureVirus);
        SubscribeLocalEvent<PartialParalysisComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);

        RashInitialize();
    }

    private void OnRefreshSpeed(Entity<PartialParalysisComponent> entity, ref RefreshMovementSpeedModifiersEvent ev)
    {
        if (TryComp<VirusComponent>(entity, out var virus) &&
            HasSymptom<PartialParalysisSymptom>((entity, virus)))
        {
            ev.ModifySpeed(0.65f);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VirusComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.VirusUpdateWindow != null &&
                _timedWindowSystem.IsExpired(component.VirusUpdateWindow))
            {
                _timedWindowSystem.Reset(component.VirusUpdateWindow);
                _virusUpdateQueue.Add(uid);
            }
        }

        foreach (var uid in _virusUpdateQueue)
        {
            if (!TryComp<VirusComponent>(uid, out var component))
                continue;

            UpdateVirus(uid, component);
        }

        _virusUpdateQueue.Clear();
    }

    private void OnCauseVirus(Entity<VirusComponent> entity, ref CauseVirusEvent args)
    {
        RebuildSymptoms(entity, args.SourceData);
    }

    private void OnCureVirus(Entity<VirusComponent> entity, ref CureVirusEvent args)
    {
        if (_mobState.IsDead(entity))
            return;

        // При изличении вырабатывается иммунитет
        var immun = EnsureComp<VirusImmunComponent>(entity);
        immun.StrainsId.Add(entity.Comp.Data.StrainId);
    }

    private void OnMobStateChanged(EntityUid uid, VirusComponent component, MobStateChangedEvent args)
    {
        component.PatientState = args.NewMobState;
    }

    private void UpdateVirus(EntityUid uid, VirusComponent component)
    {
        component.Data.MutationPoints += component.Data.RegenMutationPoints;

        if (CanManifestInHost((uid, component)))
        {
            foreach (var symptom in component.ActiveSymptomInstances)
            {
                symptom.OnUpdate(uid, component);
            }
        }

        if (!BaseVirusSettings.DebuffVirusMultipliers.TryGetValue(component.RegenerationType, out var regenMultiplier))
            regenMultiplier = 1.0f;

        var totalPoints = component.Data.RegenThreshold * regenMultiplier;

        if (component.PatientState is MobState.Dead)
            totalPoints = -component.Data.DamageWhenDead;

        AddThresholdPoints((uid, component), totalPoints);
    }

    private void OnComponentInit(EntityUid uid, VirusComponent component, ComponentInit args)
    {
        var whitelist = component.Data.EntityWhitelist ??= new EntityWhitelist();

        whitelist.Components ??= Array.Empty<string>();
        var compList = whitelist.Components.ToHashSet();

        compList.Add("MobState");
        compList.Add("Bloodstream");

        _timedWindowSystem.Reset(component.VirusUpdateWindow);
        whitelist.Components = compList.ToArray();

        RefreshSymptoms((uid, component));

        if (string.IsNullOrEmpty(component.Data.StrainId))
            component.Data.StrainId = GenerateStrainId();

        AddInfectedStrain(component.Data.StrainId);
        UpdateBloodVirusData((uid, component), true);
    }

    private void OnShutdown(EntityUid uid, VirusComponent component, ComponentShutdown args)
    {
        foreach (var symptom in component.ActiveSymptomInstances)
        {
            symptom.OnRemoved(uid, component);
        }

        UpdateBloodVirusData((uid, component), false);
        RemoveInfectedStrain(component.Data.StrainId);
    }

    /// <summary>
    ///     Изменяет здоровье вируса.
    /// </summary>
    public void AddThresholdPoints(Entity<VirusComponent?> host, float points = 1f)
    {
        if (!Resolve(host, ref host.Comp, false))
            return;

        if (host.Comp.Data.Threshold + points >= host.Comp.Data.MaxThreshold)
            return;

        host.Comp.Data.Threshold += points;

        if (host.Comp.Data.Threshold <= 0)
            CureVirus(host, host.Comp);
    }

    /// <summary>
    ///     Инфецируемый распространяет инфекцию вокруг себя.
    /// </summary>
    public void InfectAround(Entity<VirusComponent?> host, float range = 1f)
    {
        if (!Resolve(host, ref host.Comp, false))
            return;

        InfectAround(host, range, host.Comp);
    }

    /// <summary>
    ///     Обновляет VirusData по логике интерфейсов симптомов в компонент.
    /// </summary>
    public void RefreshSymptoms(Entity<VirusComponent?> host)
    {
        if (!Resolve(host, ref host.Comp, false))
            return;

        // Собираем активные типы симптомов из данных вируса
        var activeTypes = new HashSet<VirusSymptom>();
        if (host.Comp.Data.ActiveSymptom != null)
        {
            foreach (var protoSymptom in host.Comp.Data.ActiveSymptom)
            {
                if (_prototype.TryIndex(protoSymptom, out var symptom))
                    activeTypes.Add(symptom.SymptomType);
            }
        }

        // Удаляем симптомы, которых больше нет в ActiveSymptom
        for (var i = host.Comp.ActiveSymptomInstances.Count - 1; i >= 0; i--)
        {
            var instance = host.Comp.ActiveSymptomInstances[i];
            if (!activeTypes.Contains(instance.Type))
            {
                if (CanManifestInHost((host, host.Comp)))
                    instance.OnRemoved(host, host.Comp);
                else
                    instance.ApplyDataEffect(host.Comp.Data, false);
                host.Comp.ActiveSymptomInstances.RemoveAt(i);
            }
        }

        // Добавляем новые симптомы
        if (host.Comp.Data.ActiveSymptom != null)
        {
            foreach (var protoSymptom in host.Comp.Data.ActiveSymptom)
            {
                if (!_prototype.TryIndex(protoSymptom, out var prototype))
                    continue;

                if (host.Comp.ActiveSymptomInstances.Any(s => s.Type == prototype.SymptomType))
                    continue;

                var symptomInstance = CreateSymptomInstance(protoSymptom);
                host.Comp.ActiveSymptomInstances.Add(symptomInstance);

                if (CanManifestInHost((host, host.Comp)))
                {
                    _sawmill.Debug($"Добавлен ActiveSymptomInstance {symptomInstance.ToString()} к сущности {host.Owner}.");
                    symptomInstance.OnAdded(host, host.Comp);
                }
                else
                {
                    symptomInstance.ApplyDataEffect(host.Comp.Data, true);
                }
            }
        }
    }

    /// <summary>
    ///     Используйте RebuildSymptoms, а не RefreshSymptoms, если данные должны соответствовать источнику
    ///     Добавляет интерфейсы в компонент из симптомов VirusData.
    ///     Полностью сносим и пересобираем под VirusData из источника, иная логика может привести к ошибкам.
    /// </summary>
    public void RebuildSymptoms(Entity<VirusComponent> host, VirusData source)
    {
        var oldStrainId = host.Comp.Data.StrainId;

        for (var i = host.Comp.ActiveSymptomInstances.Count - 1; i >= 0; i--)
        {
            var instance = host.Comp.ActiveSymptomInstances[i];

            if (CanManifestInHost((host, host.Comp)))
                instance.OnRemoved(host, host.Comp);
            else
                instance.ApplyDataEffect(host.Comp.Data, false);

            host.Comp.ActiveSymptomInstances.RemoveAt(i);
        }

        host.Comp.Data = (VirusData)source.CloneForInfection();
        UpdateInfectedStrain(oldStrainId, host.Comp.Data.StrainId);

        foreach (var protoSymptom in host.Comp.Data.ActiveSymptom)
        {
            var instance = CreateSymptomInstance(protoSymptom);
            host.Comp.ActiveSymptomInstances.Add(instance);

            if (CanManifestInHost((host, host.Comp)))
            {
                _sawmill.Debug(
                    $"Добавлен ActiveSymptomInstance {instance} к сущности {host.Owner}");
                instance.OnAdded(host, host.Comp);
            }
            else
            {
                instance.ApplyDataEffect(host.Comp.Data, true);
            }
        }

        UpdateBloodVirusData(host, true);
    }

    private void UpdateBloodVirusData(Entity<VirusComponent> host, bool add)
    {
        if (!TryComp<BloodstreamComponent>(host, out var bloodstream))
            return;

        var bloodReagents = bloodstream.BloodReferenceSolution.Contents
            .Select(reagent => reagent.Reagent.Prototype)
            .ToHashSet();

        UpdateSolutionVirusData(bloodstream.BloodReferenceSolution, host.Comp, add, bloodReagents);

        if (bloodstream.BloodSolution is { } bloodSolution)
            UpdateSolutionVirusData(bloodSolution.Comp.Solution, host.Comp, add, bloodReagents);
    }

    private void UpdateSolutionVirusData(
        Solution solution,
        VirusComponent component,
        bool add,
        HashSet<string> bloodReagents)
    {
        foreach (var reagent in solution.Contents)
        {
            var reagentData = reagent.Reagent.EnsureReagentData();
            reagentData.RemoveAll(data => data is VirusData);

            if (add && bloodReagents.Contains(reagent.Reagent.Prototype))
                reagentData.Add((VirusData) component.Data.Clone());
        }
    }

    public void InfectAround(EntityUid host, float range = 1f, VirusComponent? component = null)
    {
        if (!Resolve(host, ref component, false))
            return;

        var coordinates = _transform.GetMapCoordinates(host, Transform(host));
        var wasLookupInProgress = _infectionLookupInProgress;
        var targets = wasLookupInProgress
            ? new HashSet<Entity<MobStateComponent>>()
            : _nearbyInfectionTargets;

        targets.Clear();
        _lookup.GetEntitiesInRange(coordinates, range, targets);

        if (!wasLookupInProgress)
            _infectionLookupInProgress = true;

        try
        {
            foreach (var ent in targets)
            {
                var target = ent.Owner;

                if (target == host)
                    continue;

                if (!CanAttemptInfect(target, component.Data))
                    continue;

                if (!_interaction.InRangeUnobstructed(host, target, range, CollisionGroup.Opaque))
                    continue;

                ProbInfect((host, component), target);
            }
        }
        finally
        {
            targets.Clear();

            if (!wasLookupInProgress)
                _infectionLookupInProgress = false;
        }
    }

    private bool CanAttemptInfect(EntityUid target, VirusData data)
    {
        return _tag.HasTag(target, IgnoreCanInfectTag) || CanInfect(target, data);
    }

    /// <summary>
    ///     Заразить с вероятностью.
    /// </summary>
    public void ProbInfect(Entity<VirusComponent?> host, EntityUid target)
    {
        if (!Resolve(host, ref host.Comp, false))
            return;

        ProbInfect(host.Comp.Data, target, host);
    }

    public void ProbInfect(VirusData data, EntityUid target, EntityUid? host = null)
    {
        var ev = new ProbInfectAttemptEvent(target, false, host);
        RaiseLocalEvent(target, ev);

        if (ev.Cancel)
            return;

        if (!CanInfect(target, data) && !_tag.HasTag(target, IgnoreCanInfectTag))
            return;

        if (_tag.HasTag(target, VirusAlwaysInfectableTag))
        {
            InfectEntity(data, target);
            return;
        }

        // Вычисляем шанс заражения
        var chance = GetVirusInfectionChance(target, data);

        // Бросаем шанс
        if (_random.Prob(chance))
        {
            _sawmill.Debug($"[{host}] заразил [{target}] вирусом {data.StrainId} (шанс {chance:P0})");
            InfectEntity(data, target);
        }
        else
        {
            _sawmill.Debug($"[{host}] не заразил [{target}] (шанс {chance:P0})");
        }
    }

    public void InfectEntity(Entity<VirusComponent?> source, EntityUid target)
    {
        if (!Resolve(source, ref source.Comp, false))
            return;

        InfectEntity(source.Comp.Data, target);
    }

    public void InfectEntity(VirusData data, EntityUid target)
    {
        if (HasComp<GuardianComponent>(target))
            return;

        if (TryComp<VirusComponent>(target, out var targetVirus)
            && targetVirus.Data.StrainId == data.StrainId)
        {
            MergeMedicineResistance(data, targetVirus.Data);
        }

        // Проверяем PrimaryPatient и другой штамм
        if (TryComp<PrimaryPacientComponent>(target, out var pacientComponent)
            && pacientComponent.StrainId != data.StrainId)
        {
            RemComp<PrimaryPacientComponent>(target);
        }

        // В любом случае копируем остальные данные (например, симптомы, тела и т.п.)
        EnsureComp<VirusComponent>(target);

        var ev = new CauseVirusEvent(data);
        RaiseLocalEvent(target, ev);
    }

    private void MergeMedicineResistance(VirusData source, VirusData target)
    {
        foreach (var kvp in source.MedicineResistance)
        {
            if (target.MedicineResistance.TryGetValue(kvp.Key, out var existingValue))
            {
                // Берём лучший (максимальный) коэффициент
                target.MedicineResistance[kvp.Key] = Math.Max(existingValue, kvp.Value);
            }
            else
            {
                // Если элемента нет — добавляем
                target.MedicineResistance[kvp.Key] = kvp.Value;
            }
        }

        // Также переносим недостающие элементы из target в source, если нужно
        foreach (var kvp in target.MedicineResistance)
        {
            if (!source.MedicineResistance.ContainsKey(kvp.Key))
                source.MedicineResistance[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    ///     Возможность заразиться вирусом.
    /// </summary>
    public bool CanInfect(EntityUid target, VirusComponent component)
    {
        return CanInfect(target, component.Data);
    }

    public bool CanInfect(EntityUid target, VirusData data)
    {
        if (HasComp<GuardianComponent>(target)
            || HasComp<ZombieComponent>(target)
            || HasComp<NecromorfComponent>(target)
            || HasComp<InfectionDeadComponent>(target)
            || HasComp<PendingZombieComponent>(target))
            return false;

        if (_mobState.IsDead(target))
            return false;

        if (TryComp<VirusImmunComponent>(target, out var immun) &&
            (immun.StrainsId.Contains(data.StrainId) || immun.ImmunAll))
        {
            return false;
        }

        if (TryComp<VirusComponent>(target, out var targetVirusComp))
        {
            // Сила вируса определяется по количеству симптомов
            if (targetVirusComp.Data.ActiveSymptom.Count >= data.ActiveSymptom.Count)
                return false;
        }

        if (!_whitelist.IsWhitelistPass(data.EntityWhitelist, target))
            return false;

        // Должно быть тело!
        if (TryComp<BodyComponent>(target, out var body)
            && body.Prototype != null
            && !data.BodyWhitelist.Contains(_prototype.Index(body.Prototype.Value)))
            return false;

        return true;
    }

    public string GenerateStrainId()
    {
        const int length = 6;

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        var id = new char[length];
        for (int i = 0; i < length; i++)
        {
            id[i] = chars[_random.Next(chars.Length)];
        }

        return new string(id);
    }

    public int GetInfectedCount(string strainId)
    {
        return string.IsNullOrEmpty(strainId)
            ? 0
            : _infectedByStrain.GetValueOrDefault(strainId);
    }

    private void AddInfectedStrain(string strainId)
    {
        if (string.IsNullOrEmpty(strainId))
            return;

        _infectedByStrain[strainId] = GetInfectedCount(strainId) + 1;
    }

    private void RemoveInfectedStrain(string strainId)
    {
        if (string.IsNullOrEmpty(strainId) || !_infectedByStrain.TryGetValue(strainId, out var count))
            return;

        if (count <= 1)
        {
            _infectedByStrain.Remove(strainId);
            return;
        }

        _infectedByStrain[strainId] = count - 1;
    }

    private void UpdateInfectedStrain(string oldStrainId, string newStrainId)
    {
        if (oldStrainId == newStrainId)
            return;

        RemoveInfectedStrain(oldStrainId);
        AddInfectedStrain(newStrainId);
    }

    public static bool CanAddSymptom(
        IReadOnlyCollection<ProtoId<VirusSymptomPrototype>> activeSymptoms,
        ProtoId<VirusSymptomPrototype> symptomId,
        VirusSymptomPrototype symptom,
        bool isTaipan)
    {
        if (activeSymptoms.Contains(symptomId))
            return false;

        if (symptom.TaipanOnly && !isTaipan)
            return false;

        if (symptom.RequiredSymptom is { } required &&
            !activeSymptoms.Contains(required))
            return false;

        return !symptom.BlockedBySymptoms.Any(blocked => activeSymptoms.Contains(blocked));
    }

    public VirusData GenerateVirusData(
    string strainId,
    Dictionary<DangerIndicatorSymptom, int> symptomsByDanger,
    int bodyCount)
    {
        var data = new VirusData(strainId);

        foreach (var (danger, count) in symptomsByDanger)
        {
            if (count <= 0)
                continue;

            var availableSymptoms = _prototype
                .EnumeratePrototypes<VirusSymptomPrototype>()
                .Where(p =>
                    p.DangerIndicator == danger &&
                    !p.TaipanOnly &&
                    !data.ActiveSymptom.Contains(p.ID))
                .ToList();

            if (availableSymptoms.Count == 0)
                continue;

            var toAdd = Math.Min(count, availableSymptoms.Count);

            for (var i = 0; i < toAdd; i++)
            {
                var picked = _random.PickAndTake(availableSymptoms);
                data.ActiveSymptom.Add(picked.ID);

                if (availableSymptoms.Count == 0)
                    break;
            }
        }

        if (bodyCount > 0)
        {
            var availableBodies = _prototype
                .EnumeratePrototypes<BodyPrototype>()
                .Select(p => p.ID)
                .Where(id => !BaseVirusSettings.BodyBlackList.Contains(id) && !data.BodyWhitelist.Contains(id))
                .ToList();

            if (availableBodies.Count > 0)
            {
                var toAdd = Math.Min(bodyCount, availableBodies.Count);

                for (var i = 0; i < toAdd; i++)
                {
                    var body = _random.PickAndTake(availableBodies);
                    data.BodyWhitelist.Add(body);

                    if (availableBodies.Count == 0)
                        break;
                }
            }
        }

        return data;
    }

    public void CureVirus(EntityUid uid, VirusComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        RaiseLocalEvent(uid, new CureVirusEvent(uid));

        RemComp<VirusComponent>(uid);
    }

    /// <summary>
    ///     Пытается нанести урон вирусу антибиотиком.
    ///     С каждым применением увеличивает сопротивление к этому антибиотику.
    ///     Сопротивление никогда не опускается ниже DefaultMedicineResistance.
    /// </summary>
    public void ApplyMedicineDamage(
        Entity<VirusComponent?> target,
        ProtoId<ReagentPrototype> medicine,
        float baseDamage,
        float resistanceIncrease = 0.05f,
        float maxResistance = 0.9f)
    {
        if (!Resolve(target, ref target.Comp, false))
            return;

        var data = target.Comp.Data;

        // Получаем текущее сопротивление (не ниже дефолтного)
        if (!data.MedicineResistance.TryGetValue(medicine, out var resistance))
        {
            resistance = data.DefaultMedicineResistance;
        }
        else
        {
            resistance = Math.Max(resistance, data.DefaultMedicineResistance);
        }

        // Считаем фактический урон
        var damageMultiplier = Math.Clamp(1f - resistance, 0f, 1f);
        var finalDamage = baseDamage * damageMultiplier;

        if (finalDamage > 0f)
            AddThresholdPoints(target, -finalDamage);

        // Увеличиваем сопротивление
        var newResistance = resistance + resistanceIncrease;

        newResistance = Math.Clamp(
            newResistance,
            data.DefaultMedicineResistance,
            maxResistance
        );

        data.MedicineResistance[medicine] = newResistance;
    }

    private float GetVirusInfectionChance(EntityUid target, VirusComponent component)
    {
        return GetVirusInfectionChance(target, component.Data);
    }

    private float GetVirusInfectionChance(EntityUid target, VirusData data)
    {
        var resistanceQuery = new VirusResistanceQueryEvent(ProtectiveSlots);
        RaiseLocalEvent(target, resistanceQuery);

        var finalChance = data.Infectivity * resistanceQuery.TotalCoefficient;

        // от 0 до 100%
        finalChance = Math.Clamp(finalChance, 0f, 1.0f);

        return finalChance;
    }

    /// <summary>
    ///     Нужно добавить новый тип вируса в этот switch.
    /// </summary>
    public IVirusSymptom CreateSymptomInstance(ProtoId<VirusSymptomPrototype> symptomId)
    {
        if (!_prototype.TryIndex(symptomId, out var proto))
            throw new Exception($"No prototype for symptom {symptomId}");

        var newWindow = new TimedWindow(TimeSpan.FromSeconds(proto.MinInterval), TimeSpan.FromSeconds(proto.MaxInterval));
        return proto.SymptomType switch
        {
            VirusSymptom.Cough =>
                new CoughSymptom(newWindow),

            VirusSymptom.Vomit =>
                new VomitSymptom(newWindow),

            VirusSymptom.Rash =>
                new RashSymptom(newWindow),

            VirusSymptom.Drowsiness =>
                new DrowsinessSymptom(newWindow),

            VirusSymptom.Necrosis =>
                new NecrosisSymptom(newWindow),

            VirusSymptom.Zombification =>
                new ZombificationSymptom(newWindow),

            VirusSymptom.LowComplexityChange =>
                new LowComplexityChangeSymptom(newWindow),

            VirusSymptom.MedComplexityChange =>
                new MedComplexityChangeSymptom(newWindow),

            VirusSymptom.LowPostMortemResistance =>
                new LowPostMortemResistanceSymptom(newWindow),

            VirusSymptom.MedPostMortemResistance =>
                new MedPostMortemResistanceSymptom(newWindow),

            VirusSymptom.LowViralRegeneration =>
                new LowViralRegenerationSymptom(newWindow),

            VirusSymptom.MedViralRegeneration =>
                new MedViralRegenerationSymptom(newWindow),

            VirusSymptom.LowMutationAcceleration =>
                new LowMutationAccelerationSymptom(newWindow),

            VirusSymptom.MedMutationAcceleration =>
                new MedMutationAccelerationSymptom(newWindow),

            VirusSymptom.LowPathogenFortress =>
                new LowPathogenFortressSymptom(newWindow),

            VirusSymptom.MedPathogenFortress =>
                new MedPathogenFortressSymptom(newWindow),

            VirusSymptom.LowChemicalAdaptation =>
                new LowChemicalAdaptationSymptom(newWindow),

            VirusSymptom.MedChemicalAdaptation =>
                new MedChemicalAdaptationSymptom(newWindow),

            VirusSymptom.AggressiveTransmission =>
                new AggressiveTransmissionSymptom(newWindow),

            VirusSymptom.NeuroSpike =>
                new NeuroSpikeSymptom(newWindow),

            VirusSymptom.VocalDisruption =>
                new VocalDisruptionSymptom(newWindow),

            VirusSymptom.Blindable =>
                new BlindableSymptom(newWindow),

            VirusSymptom.ParalyzedLegs =>
                new ParalyzedLegsSymptom(newWindow),

            VirusSymptom.PartialParalysis =>
                new PartialParalysisSymptom(newWindow),

            VirusSymptom.RespiratoryFailure =>
                new RespiratoryFailureSymptom(newWindow),

            VirusSymptom.VascularRupture =>
                new VascularRuptureSymptom(newWindow),

            VirusSymptom.EnhancedNecrosis =>
                new EnhancedNecrosisSymptom(newWindow),

            VirusSymptom.Necromutation =>
                new NecromutationSymptom(newWindow),

            _ => throw new ArgumentOutOfRangeException(
                nameof(proto.SymptomType),
                $"Unknown virus symptom {proto.SymptomType}"
            )
        };
    }

    public bool TryGetSymptom<T>(Entity<VirusComponent?> entity, out T? symptom)
    where T : class, IVirusSymptom
    {
        symptom = null;

        if (!Resolve(entity, ref entity.Comp, false))
        {
            _sawmill.Warning($"Entity {entity.Owner} не имеет компонента VirusComponent, невозможно получить симптом {typeof(T).Name}.");
            return default!;
        }

        symptom = entity.Comp.ActiveSymptomInstances.OfType<T>().FirstOrDefault();
        return symptom != null;
    }

    public T EnsureSymptom<T>(Entity<VirusComponent?> entity)
    where T : IVirusSymptom
    {
        if (!Resolve(entity, ref entity.Comp, false))
        {
            _sawmill.Warning($"Entity {entity.Owner} не имеет компонента VirusComponent, невозможно добавить симптом {typeof(T).Name}.");
            return default!;
        }

        // Ищем симптом нужного типа
        var existing = entity.Comp.ActiveSymptomInstances.OfType<T>().FirstOrDefault();
        if (existing != null)
            return existing;

        return AddSymptom<T>(entity);
    }

    public T AddSymptom<T>(Entity<VirusComponent?> entity)
    where T : IVirusSymptom
    {
        if (!Resolve(entity, ref entity.Comp, false))
        {
            _sawmill.Warning($"Entity {entity.Owner} не имеет компонента VirusComponent, невозможно добавить симптом {typeof(T).Name}.");
            return default!;
        }

        if (entity.Comp.ActiveSymptomInstances == null)
            entity.Comp.ActiveSymptomInstances = new List<IVirusSymptom>();

        // создаём симптом с таймером
        var symptom = (T)Activator.CreateInstance(typeof(T), this, _timing, DefaultSymptomWindow)!;

        if (entity.Comp.ActiveSymptomInstances.Any(s => s.Type == symptom.Type))
            return symptom; // возвращаем существующий симптом, если он уже есть

        entity.Comp.ActiveSymptomInstances.Add(symptom);

        if (CanManifestInHost((entity.Owner, entity.Comp)))
            symptom.OnAdded(entity.Owner, entity.Comp);
        else
            symptom.ApplyDataEffect(entity.Comp.Data, true);

        _sawmill.Debug($"Добавлен симптом {typeof(T).Name} к сущности {entity.Owner}.");

        return symptom;
    }

    public void RemoveSymptom<T>(Entity<VirusComponent?> entity)
    where T : IVirusSymptom
    {
        if (!Resolve(entity, ref entity.Comp, false))
        {
            _sawmill.Warning($"Entity {entity.Owner} не имеет компонента VirusComponent, невозможно удалить симптом {typeof(T).Name}.");
            return;
        }

        if (entity.Comp.ActiveSymptomInstances == null)
            return;

        var symptom = entity.Comp.ActiveSymptomInstances.FirstOrDefault(s => s is T);
        if (symptom == null)
            return;

        if (CanManifestInHost((entity.Owner, entity.Comp)))
            symptom.OnRemoved(entity.Owner, entity.Comp);
        else
            symptom.ApplyDataEffect(entity.Comp.Data, false);

        entity.Comp.ActiveSymptomInstances.Remove(symptom);

        _sawmill.Debug($"Удалён симптом {typeof(T).Name} у сущности {entity.Owner}.");
    }

}
