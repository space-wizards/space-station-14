// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Body.Systems;
using Content.Server.Cloning;
using Content.Shared.Cloning.Events;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead;
using Content.Server.Emoting.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.DeadSpace.Abilities.ReleaseGasPerSecond.Components;
using Content.Shared.Slippery;
using Content.Shared.DeadSpace.Abilities.ExplosionAbility.Components;
using Content.Shared.DeadSpace.Abilities.Invisibility.Components;
using Content.Shared.DeadSpace.Demons.Abilities.Components;
using Content.Shared.Charges.Components;
using Content.Shared.Electrocution;
using Content.Server.DeadSpace.Components.NightVision;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Mobs;
using Content.Shared.Damage.Components;
using Content.Shared.Humanoid;
using Content.Shared.NPC.Components;
using Content.Shared.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.DeadSpace.Necromorphs.InfectionDead;

public sealed partial class NecromorfSystem : SharedInfectionDeadSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    private static readonly Color NecromorphVisionColor = new(110f / 255f, 0f, 0f, 0.067f);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NecromorfComponent, ComponentStartup>(OnStartup);
//        SubscribeLocalEvent<NecromorfComponent, EmoteEvent>(OnEmote, before:
//            new[] { typeof(VocalSystem), typeof(BodyEmotesSystem) });
        SubscribeLocalEvent<NecromorfComponent, TryingToSleepEvent>(OnSleepAttempt);
        SubscribeLocalEvent<NecromorfComponent, GetCharactedDeadIcEvent>(OnGetCharacterDeadIC);
        SubscribeLocalEvent<NecromorfComponent, IsEquippingAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<NecromorfComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<NecromorfComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<NecromorfComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<NecromorfComponent, CloningEvent>(
            OnNecromorfCloning,
            after: [typeof(CloningSystem)]);
    }

    private void OnRefreshSpeed(EntityUid uid, NecromorfComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.MovementSpeedMultiply * component.StrainData.SpeedMulty, component.MovementSpeedMultiply * component.StrainData.SpeedMulty);
    }

    private void OnMeleeHit(EntityUid uid, NecromorfComponent component, MeleeHitEvent args)
    {
        args.BonusDamage = args.BaseDamage * component.StrainData.DamageMulty;

        foreach (var entity in args.HitEntities)
        {
            if (args.User == entity)
                continue;

            if (!TryComp<MobStateComponent>(entity, out var mobState))
                continue;

            if (!_mobState.IsDead(entity, mobState) && !HasComp<NecromorfComponent>(entity) && VirusEffectsConditions.HasEffect(component.StrainData.Effects, VirusEffects.Vampirism))
                _damageable.TryChangeDamage(uid, VirusEffectsConditions.HealingOnBite, true, false);
        }

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        // Heal
        var necroQuery = EntityQueryEnumerator<NecromorfComponent, DamageableComponent, MobStateComponent>();
        while (necroQuery.MoveNext(out var uid, out var comp, out var damage, out var mobState))
        {
            // Process only once per second
            if (comp.NextTick + TimeSpan.FromSeconds(1) > curTime)
                continue;

            comp.NextTick = curTime;

            if (_mobState.IsDead(uid, mobState))
                continue;

            var multiplier = _mobState.IsCritical(uid, mobState)
                ? comp.PassiveHealingCritMultiplier
                : 1f;

            // Gradual healing for living Necromorfs.
            _damageable.TryChangeDamage(uid, comp.PassiveHealing * multiplier, true, false);
        }
    }

    private void OnSleepAttempt(EntityUid uid, NecromorfComponent component, ref TryingToSleepEvent args)
    {
        args.Cancelled = true;
    }

    private void OnGetCharacterDeadIC(EntityUid uid, NecromorfComponent component, ref GetCharactedDeadIcEvent args)
    {
        args.Dead = true;
    }

    private void OnEquipAttempt(EntityUid uid, NecromorfComponent component, IsEquippingAttemptEvent args)
    {
        if (!component.IsCanUseInventory)
        {
            args.Cancel();
            return;
        }
    }

    private void OnStartup(EntityUid uid, NecromorfComponent component, ComponentStartup args)
    {
//       _protoManager.TryIndex(component.EmoteSoundsId, out component.EmoteSounds);
//   }
//
//   private void OnEmote(EntityUid uid, NecromorfComponent component, ref EmoteEvent args)
//   {
//       if (args.Handled)
//           return;
//
//       args.Handled = _chat.TryPlayEmoteSound(uid, component.EmoteSounds, args.Emote);
    }

    private void OnMindRemoved(Entity<NecromorfComponent> ent, ref MindRemovedMessage args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        EnsureNecromorphGhostRole(ent);
    }

    private void OnNecromorfCloning(Entity<NecromorfComponent> ent, ref CloningEvent args)
    {
        UnNecroficate(ent.Owner, args.CloneUid, ent.Comp);
    }

    private bool UnNecroficate(EntityUid source, EntityUid target, NecromorfComponent? necromorfComp)
    {
        if (!Resolve(source, ref necromorfComp))
            return false;

        foreach (var (layer, info) in necromorfComp.BeforeNecroficationCustomBaseLayers)
        {
            _humanoidAppearance.SetBaseLayerColor(target, layer, info.Color);
            _humanoidAppearance.SetBaseLayerId(target, layer, info.Id);
        }

        if (TryComp<HumanoidAppearanceComponent>(target, out var appcomp))
            appcomp.EyeColor = necromorfComp.BeforeNecroficationEyeColor;

        _humanoidAppearance.SetSkinColor(target, necromorfComp.BeforeNecroficationSkinColor, false);
        _bloodstream.ChangeBloodReagents(target, necromorfComp.BeforeNecroficationBloodReagents);

        if (TryComp<NpcFactionMemberComponent>(target, out _))
        {
            _faction.ClearFactions(target);
            foreach (var faction in necromorfComp.BeforeNecroficationFactions)
            {
                _faction.AddFaction(target, faction);
            }
        }

        RestoreVocalComponent(target, necromorfComp);

        return true;
    }

    private void RestoreVocalComponent(EntityUid target, NecromorfComponent necromorfComp)
    {
        if (!necromorfComp.BeforeNecroficationHadVocal)
        {
            RemComp<VocalComponent>(target);
            return;
        }

        var vocalComp = EnsureComp<VocalComponent>(target);
        vocalComp.Sounds = necromorfComp.BeforeNecroficationVocalSounds is null
            ? null
            : new(necromorfComp.BeforeNecroficationVocalSounds);
        vocalComp.ScreamId = necromorfComp.BeforeNecroficationScreamId;
        vocalComp.Wilhelm = necromorfComp.BeforeNecroficationWilhelm;
        vocalComp.WilhelmProbability = necromorfComp.BeforeNecroficationWilhelmProbability;
        vocalComp.EmoteSounds = null;

        var sex = CompOrNull<HumanoidAppearanceComponent>(target)?.Sex ?? Sex.Unsexed;
        if (vocalComp.Sounds?.TryGetValue(sex, out var emoteSounds) == true &&
            _protoManager.HasIndex(emoteSounds))
        {
            vocalComp.EmoteSounds = emoteSounds;
        }

        Dirty(target, vocalComp);
    }

    public void ApplyVirusStrain(EntityUid uid, NecromorfComponent component)
    {
        if (!HasComp<PullerComponent>(uid) && VirusEffectsConditions.HasEffect(component.StrainData.Effects, VirusEffects.Pulling))
        {
            var puller = new PullerComponent(false);
            AddComp(uid, puller);
        }

        if (!HasComp<StunAttackComponent>(uid) && VirusEffectsConditions.HasEffect(component.StrainData.Effects, VirusEffects.StunAttack))
            AddComp<StunAttackComponent>(uid);

        //if (!HasComp<DemonDashComponent>(uid) && VirusEffectsConditions.HasEffect(component.StrainData.Effects, VirusEffects.Dash))
        //{
        //    AddComp<DemonDashComponent>(uid);
        //    if (!HasComp<LimitedChargesComponent>(uid))
        //        AddComp<LimitedChargesComponent>(uid);
        //}

        if (!HasComp<InsulatedComponent>(uid) && VirusEffectsConditions.HasEffect(component.StrainData.Effects, VirusEffects.Insulated))
            AddComp<InsulatedComponent>(uid);

        if (!HasComp<NightVisionComponent>(uid) && VirusEffectsConditions.HasEffect(component.StrainData.Effects, VirusEffects.NightVision))
        {
            var nvComp = new NightVisionComponent(NecromorphVisionColor);
            AddComp(uid, nvComp);
        }

        if (!HasComp<ReleaseGasPerSecondComponent>(uid) && VirusEffectsConditions.HasEffect(component.StrainData.Effects, VirusEffects.EmitGas))
        {
            ReleaseGasPerSecondComponent rgpsc = new ReleaseGasPerSecondComponent();
            rgpsc.GasID = 9;
            AddComp(uid, rgpsc);
        }

        if (!HasComp<NoSlipComponent>(uid) && VirusEffectsConditions.HasEffect(component.StrainData.Effects, VirusEffects.NoSlip))
            AddComp<NoSlipComponent>(uid);

        if (!HasComp<ExplosionAbilityComponent>(uid) && VirusEffectsConditions.HasEffect(component.StrainData.Effects, VirusEffects.Explosion))
            AddComp<ExplosionAbilityComponent>(uid);

        if (!HasComp<InvisibilityComponent>(uid) && VirusEffectsConditions.HasEffect(component.StrainData.Effects, VirusEffects.Invisability))
            AddComp<InvisibilityComponent>(uid);

        if (_mobThreshold.TryGetThresholdForState(uid, MobState.Dead, out var deadThreshold))
            _mobThreshold.SetMobStateThreshold(uid, deadThreshold.Value * component.StrainData.HpMulty, MobState.Dead);

        if (_mobThreshold.TryGetThresholdForState(uid, MobState.PreCritical, out var preCritThreshold))
            _mobThreshold.SetMobStateThreshold(uid, preCritThreshold.Value * component.StrainData.HpMulty, MobState.PreCritical);

        if (_mobThreshold.TryGetThresholdForState(uid, MobState.Critical, out var critThreshold))
            _mobThreshold.SetMobStateThreshold(uid, critThreshold.Value * component.StrainData.HpMulty, MobState.Critical);

        if (TryComp<StaminaComponent>(uid, out var stamina))
            stamina.CritThreshold *= component.StrainData.StaminaMulty;

        float totalScore = 0f;
        totalScore += Normalize(component.StrainData.DamageMulty, VirusEffectsConditions.MinDamageMulty, VirusEffectsConditions.MaxDamageMulty);
        totalScore += Normalize(component.StrainData.StaminaMulty, VirusEffectsConditions.MinStaminaMulty, VirusEffectsConditions.MaxStaminaMulty);
        totalScore += Normalize(component.StrainData.HpMulty, VirusEffectsConditions.MinHpMulty, VirusEffectsConditions.MaxHpMulty);
        totalScore += Normalize(component.StrainData.SpeedMulty, VirusEffectsConditions.MinSpeedMulty, VirusEffectsConditions.MaxSpeedMulty);

        float intensity = Math.Clamp(totalScore / 4f, 0f, 1f);

        // Интерполяция от белого (1,1,1) до красного (1,0,0)
        float r = 1f;
        float g = 1f - intensity;
        float b = 1f - intensity;

        Color skinColor = new Color(r, g, b);
        component.StrainData.SkinColor = skinColor;

        if (TryComp<HumanoidAppearanceComponent>(uid, out var huApComp))
            _humanoidAppearance.SetSkinColor(uid, component.StrainData.SkinColor, verify: false, humanoid: huApComp);

        _movement.RefreshMovementSpeedModifiers(uid);
    }


    /// <summary>
    /// mutationStrength = 0.1 - слабая, = 2 - сильная
    /// </summary>
    public void MutateVirus(EntityUid uid, float mutationStrength, bool isStableMutation, NecromorfComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.IsMutated)
            return;

        var randDamageMulty = 0f;
        var randStaminaMulty = 0f;
        var randHpMulty = 0f;
        var randSpeedMulty = 0f;

        if (!isStableMutation)
        {
            randDamageMulty = _random.NextFloat(-mutationStrength, mutationStrength);
            randStaminaMulty = _random.NextFloat(-mutationStrength, mutationStrength);
            randHpMulty = _random.NextFloat(-mutationStrength, mutationStrength);
            randSpeedMulty = _random.NextFloat(-mutationStrength, mutationStrength);
        }
        else
        {
            randDamageMulty = _random.NextFloat(0f, mutationStrength);
            randStaminaMulty = _random.NextFloat(0f, mutationStrength);
            randHpMulty = _random.NextFloat(0f, mutationStrength);
            randSpeedMulty = _random.NextFloat(0f, mutationStrength);
        }


        component.StrainData.DamageMulty = Math.Clamp(component.StrainData.DamageMulty + randDamageMulty, VirusEffectsConditions.MinDamageMulty, VirusEffectsConditions.MaxDamageMulty);
        component.StrainData.StaminaMulty = Math.Clamp(component.StrainData.StaminaMulty + randStaminaMulty, VirusEffectsConditions.MinStaminaMulty, VirusEffectsConditions.MaxStaminaMulty);
        component.StrainData.HpMulty = Math.Clamp(component.StrainData.HpMulty + randHpMulty, VirusEffectsConditions.MinHpMulty, VirusEffectsConditions.MaxHpMulty);
        component.StrainData.SpeedMulty = Math.Clamp(component.StrainData.SpeedMulty + randSpeedMulty, VirusEffectsConditions.MinSpeedMulty, VirusEffectsConditions.MaxSpeedMulty);

        // Кол-во попыток добавления эффектов
        int attempts = (int)MathF.Ceiling(mutationStrength * 4f);

        for (int i = 0; i < attempts; i++)
        {
            var effect = GetRandomEffectExcludingCurrent(component.StrainData.Effects);

            var weight = VirusEffectsConditions.Weights[effect];

            float chance = weight * mutationStrength;

            if (_random.Prob(chance))
            {
                component.StrainData.Effects = VirusEffectsConditions.AddEffect(component.StrainData.Effects, effect);
                break; // Ограничиваем одним успешным эффектом за попытку
            }
        }

        ApplyVirusStrain(uid, component);

        component.IsMutated = true;
    }

    private float Normalize(float value, float min, float max)
    {
        return Math.Clamp((value - min) / (max - min), 0f, 1f);
    }

    public VirusEffects GetRandomEffectExcludingCurrent(VirusEffects currentEffects)
    {
        var allEffects = Enum.GetValues<VirusEffects>();

        var availableEffects = new List<VirusEffects>();

        foreach (var effect in allEffects)
        {
            if (effect == VirusEffects.None)
                continue;

            if (!currentEffects.HasFlag(effect))
                availableEffects.Add(effect);
        }

        if (availableEffects.Count == 0)
            return VirusEffects.None;

        int index = _random.Next(availableEffects.Count);
        return availableEffects[index];
    }

}
