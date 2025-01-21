using Content.Server.Objectives.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mind;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Prototypes;
using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Starlight.ItemSwitch;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.UserInterface;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Starlight.Antags.Abductor;

public sealed partial class AbductorSystem : SharedAbductorSystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;
    [Dependency] private readonly SharedItemSwitchSystem _itemSwitch = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public void InitializeConsole()
    {
        SubscribeLocalEvent<AbductorConsoleComponent, BeforeActivatableUIOpenEvent>(OnBeforeActivatableUIOpen);
        SubscribeLocalEvent<AbductConditionComponent, ObjectiveGetProgressEvent>(OnAbductGetProgress);

        Subs.BuiEvents<AbductorConsoleComponent>(AbductorConsoleUIKey.Key, subs => subs.Event<AbductorAttractBuiMsg>(OnAttractBuiMsg));
        Subs.BuiEvents<AbductorConsoleComponent>(AbductorConsoleUIKey.Key, subs => subs.Event<AbductorCompleteExperimentBuiMsg>(OnCompleteExperimentBuiMsg));
        Subs.BuiEvents<AbductorConsoleComponent>(AbductorConsoleUIKey.Key, subs => subs.Event<AbductorVestModeChangeBuiMsg>(OnVestModeChangeBuiMsg));
        Subs.BuiEvents<AbductorConsoleComponent>(AbductorConsoleUIKey.Key, subs => subs.Event<AbductorLockBuiMsg>(OnVestLockBuiMsg));
        SubscribeLocalEvent<AbductorComponent, AbductorAttractDoAfterEvent>(OnDoAfterAttract);
    }
    private void OnAbductGetProgress(Entity<AbductConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = AbductProgress(ent, _number.GetTarget(ent.Owner));
    }

    private float AbductProgress(Entity<AbductConditionComponent> ent, int target)
    {
        if (!TryComp<AbductorScientistComponent>(ent, out var scientistComp) && !TryComp<AbductorAgentComponent>(ent, out var agentComp))
            if (scientistComp != null
                    && TryComp<MindContainerComponent>(scientistComp.Agent, out var agentMindContainer)
                    && agentMindContainer.Mind.HasValue
                    && TryComp<MindComponent>(agentMindContainer.Mind.Value, out var agentMind)
                    && agentMind.Objectives.FirstOrDefault(HasComp<AbductConditionComponent>) is EntityUid agentObjId
                    && TryComp<AbductConditionComponent>(agentObjId, out var agentAbducted))
                if (agentAbducted.Abducted > ent.Comp.Abducted)
                    ent.Comp.Abducted = agentAbducted.Abducted;
            else if (agentComp != null                     
                    && TryComp<MindContainerComponent>(agentComp.Scientist, out var scientistMindContainer)
                    && scientistMindContainer.Mind.HasValue
                    && TryComp<MindComponent>(scientistMindContainer.Mind.Value, out var scientistMind)
                    && scientistMind.Objectives.FirstOrDefault(HasComp<AbductConditionComponent>) is EntityUid scientistObjId
                    && TryComp<AbductConditionComponent>(scientistObjId, out var scientistAbducted))
                if (scientistAbducted.Abducted > ent.Comp.Abducted)
                    ent.Comp.Abducted = scientistAbducted.Abducted;
                
        return target == 0 ? 1f : MathF.Min(ent.Comp.Abducted / (float)target, 1f);
    }
        
    private void OnVestModeChangeBuiMsg(EntityUid uid, AbductorConsoleComponent component, AbductorVestModeChangeBuiMsg args)
    {
        if (component.Armor != null)
            _itemSwitch.Switch(GetEntity(component.Armor.Value), args.Mode);
    }
    
    private void OnVestLockBuiMsg(Entity<AbductorConsoleComponent> ent, ref AbductorLockBuiMsg args)
    {
        if (ent.Comp.Armor != null && GetEntity(ent.Comp.Armor.Value) is EntityUid armor)
            if (TryComp<UnremoveableComponent>(armor, out var unremoveable))
                RemComp(armor, unremoveable);
            else
                AddComp<UnremoveableComponent>(armor);
    }

    private void OnCompleteExperimentBuiMsg(EntityUid uid, AbductorConsoleComponent component, AbductorCompleteExperimentBuiMsg args)
    {
        if (component.Experimentator != null
            && GetEntity(component.Experimentator) is EntityUid experimentatorId
            && TryComp<AbductorExperimentatorComponent>(experimentatorId, out var experimentatorComp))
        {
            var container = _container.GetContainer(experimentatorId, experimentatorComp.ContainerId);
            var victim = container.ContainedEntities.FirstOrDefault(HasComp<AbductorVictimComponent>);
            if (victim != default && TryComp(victim, out AbductorVictimComponent? victimComp))
            {
                if (victimComp.Organ != AbductorOrganType.None
                    && TryComp<MindContainerComponent>(args.Actor, out var mindContainer)
                    && mindContainer.Mind.HasValue
                    && TryComp<MindComponent>(mindContainer.Mind.Value, out var mind)
                    && mind.Objectives.FirstOrDefault(HasComp<AbductConditionComponent>) is EntityUid objId
                    && TryComp<AbductConditionComponent>(objId, out var condition)
                    && !condition.AbductedHashs.Contains(GetNetEntity(victim)))
                {
                    condition.AbductedHashs.Add(GetNetEntity(victim));
                    condition.Abducted++;
                }
                _audioSystem.PlayPvs("/Audio/Voice/Human/wilhelm_scream.ogg", experimentatorId);

                if (victimComp.Position is not null)
                    _xformSys.SetCoordinates(victim, victimComp.Position.Value);
            }
        }
    }

    private void OnAttractBuiMsg(Entity<AbductorConsoleComponent> ent, ref AbductorAttractBuiMsg args)
    {
        if (ent.Comp.Target == null || ent.Comp.AlienPod == null || ent.Comp.Dispencer == null) return;
        var target = GetEntity(ent.Comp.Target.Value);
        EnsureComp<TransformComponent>(target, out var xform);
        var effectEnt = SpawnAttachedTo(_teleportationEffectEntity, xform.Coordinates);
        _xformSys.SetParent(effectEnt, target);
        EnsureComp<TimedDespawnComponent>(effectEnt, out var despawnEffectEntComp);
        despawnEffectEntComp.Lifetime = 3.0f;
        _audioSystem.PlayPvs("/Audio/_Starlight/Misc/alien_teleport.ogg", effectEnt);
        
        var telepad = GetEntity(ent.Comp.AlienPod.Value);
        var telepadXform = EnsureComp<TransformComponent>(telepad);
        var dispencerXform = EnsureComp<TransformComponent>(GetEntity(ent.Comp.Dispencer.Value));
        var effect = _entityManager.SpawnEntity(_teleportationEffect, telepadXform.Coordinates);
        EnsureComp<TimedDespawnComponent>(effect, out var despawnComp);
        despawnComp.Lifetime = 3.0f;
        _audioSystem.PlayPvs("/Audio/_Starlight/Misc/alien_teleport.ogg", effect);

        var @event = new AbductorAttractDoAfterEvent(GetNetCoordinates(telepadXform.Coordinates), GetNetEntity(target), GetNetCoordinates(dispencerXform.Coordinates));
        ent.Comp.Target = null;
        var doAfter = new DoAfterArgs(EntityManager, args.Actor, TimeSpan.FromSeconds(3), @event, args.Actor)
        {
            BreakOnDamage = false,
            BreakOnDropItem = false,
            BreakOnHandChange = false,
            BreakOnMove = false,
            BreakOnWeightlessMove = false,
        };
        _doAfter.TryStartDoAfter(doAfter);
    }
    private void OnDoAfterAttract(Entity<AbductorComponent> ent, ref AbductorAttractDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;
        
        var victim = GetEntity(args.Victim);
        if (_pullingSystem.IsPulling(victim))
        {
            if (!TryComp<PullerComponent>(victim, out var pullerComp)
                || pullerComp.Pulling == null
                || !TryComp<PullableComponent>(pullerComp.Pulling.Value, out var pullableComp)
                || !_pullingSystem.TryStopPull(pullerComp.Pulling.Value, pullableComp)) return;
        }
        if (_pullingSystem.IsPulled(victim))
        {
            if (!TryComp<PullableComponent>(victim, out var pullableComp)
                || !_pullingSystem.TryStopPull(victim, pullableComp)) return;
        }
        
        if (!HasComp<AbductorComponent>(victim))
        {
            var organPrototypes = _prototypeManager.EnumeratePrototypes<EntityPrototype>()
                .Where(p => p.HasComponent<AbductorOrganComponent>()) 
                .Select(p => p.ID.ToString())
                .Order()
                .ToList();
            Spawn(_random.Pick(organPrototypes), GetCoordinates(args.Dispencer));
        }
        
        _xformSys.SetCoordinates(victim, GetCoordinates(args.TargetCoordinates));
    }
    private void OnBeforeActivatableUIOpen(Entity<AbductorConsoleComponent> ent, ref BeforeActivatableUIOpenEvent args) => UpdateGui(ent.Comp.Target, ent);
    
    public void SyncAbductors(Entity<AbductorConsoleComponent> ent)
    {
        if (ent.Comp.Agent != null && ent.Comp.Scientist != null)
            if (TryComp<AbductorScientistComponent>(ent.Comp.Scientist, out var scientistComp) && TryComp<AbductorAgentComponent>(ent.Comp.Agent, out var agentComp))
            {
                agentComp.Scientist = ent.Comp.Scientist;
                scientistComp.Agent = ent.Comp.Agent;
            }
    }

    protected override void UpdateGui(NetEntity? target, Entity<AbductorConsoleComponent> computer)
    {
        string? targetName = null;
        string? victimName = null;
        if (target.HasValue && TryComp(GetEntity(target.Value), out MetaDataComponent? metadata))
            targetName = metadata?.EntityName;

        if (computer.Comp.AlienPod == null)
        {
            var xform = EnsureComp<TransformComponent>(computer.Owner);
            var alienpad = _entityLookup.GetEntitiesInRange<AbductorAlienPadComponent>(xform.Coordinates, 4, LookupFlags.Approximate | LookupFlags.Dynamic)
                .FirstOrDefault().Owner;
            if (alienpad != default)
                computer.Comp.AlienPod = GetNetEntity(alienpad);
        }

        if (computer.Comp.Experimentator == null)
        {
            var xform = EnsureComp<TransformComponent>(computer.Owner);
            var experimentator = _entityLookup.GetEntitiesInRange<AbductorExperimentatorComponent>(xform.Coordinates, 4, LookupFlags.Approximate | LookupFlags.Dynamic)
                .FirstOrDefault().Owner;
            if (experimentator != default)
                computer.Comp.Experimentator = GetNetEntity(experimentator);
        }
        
        if (computer.Comp.Dispencer == null)
        {
            var xform = EnsureComp<TransformComponent>(computer.Owner);
            var dispencer = _entityLookup.GetEntitiesInRange<AbductorDispencerComponent>(xform.Coordinates, 4, LookupFlags.Approximate | LookupFlags.Dynamic)
                .FirstOrDefault().Owner;
            if (dispencer != default)
                computer.Comp.Dispencer = GetNetEntity(dispencer);
        }

        if (computer.Comp.Experimentator != null
            && GetEntity(computer.Comp.Experimentator) is EntityUid experimentatorId
            && TryComp<AbductorExperimentatorComponent>(experimentatorId, out var experimentatorComp))
        {
            var container = _container.GetContainer(experimentatorId, experimentatorComp.ContainerId);
            var victim = container.ContainedEntities.FirstOrDefault(e => HasComp<AbductorVictimComponent>(e));
            if (victim != default && TryComp(victim, out MetaDataComponent? victimMetadata))
                victimName = victimMetadata?.EntityName;
        }
        
        var armorLock = false;
        
        if (computer.Comp.Armor != null && HasComp<UnremoveableComponent>(GetEntity(computer.Comp.Armor.Value)))
            armorLock = true;

        _uiSystem.SetUiState(computer.Owner, AbductorConsoleUIKey.Key, new AbductorConsoleBuiState()
        {
            Target = target,
            TargetName = targetName,
            VictimName = victimName,
            AlienPadFound = computer.Comp.AlienPod != default,
            ExperimentatorFound = computer.Comp.Experimentator != default,
            DispencerFound = computer.Comp.Dispencer != default,
            ArmorFound = computer.Comp.Armor != default,
            ArmorLocked = armorLock
        });
    }
}
