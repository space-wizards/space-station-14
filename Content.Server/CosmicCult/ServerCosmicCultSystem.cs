using System.Linq;
using Content.Server.Antag;
using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Components;
using Content.Shared.CosmicCult.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Movement.Systems;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;

namespace Content.Server.CosmicCult;

public sealed partial class ServerCosmicCultSystem : CosmicCultSystem
{
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private MovementModStatusSystem _movementMod = default!;
    // [Dependency] private ESEntityTimerSystem _entityTimer = default!;
    [Dependency] private StatusEffectsSystem _status = default!;

    // public override void Initialize()
    // {
    //     base.Initialize();
    //
    //     SubscribeLocalEvent<CosmicCultistComponent, ComponentInit>(OnStartCultist);
    //
    //     SubscribeLocalEvent<CosmicImposingComponent, ComponentInit>(OnStartImposition);
    //     SubscribeLocalEvent<CosmicImposingComponent, ComponentRemove>(OnEndImposition);
    //     SubscribeLocalEvent<InfluenceStrideComponent, ComponentInit>(OnStartInfluenceStride);
    //     SubscribeLocalEvent<InfluenceStrideComponent, ComponentRemove>(OnEndInfluenceStride);
    //
    //     SubscribeLocalEvent<InfluenceStrideComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);
    //     SubscribeLocalEvent<CosmicImposingComponent, RefreshMovementSpeedModifiersEvent>(OnImpositionMoveSpeed);
    // }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var finaleQuery = EntityQueryEnumerator<InfluenceVitalityComponent>();
        while (finaleQuery.MoveNext(out var uid, out var comp))
        {
            if (Timing.CurTime >= comp.CheckTimer)
            {

                _damage.TryChangeDamage(uid, comp.Healing * -1);
                comp.CheckTimer = Timing.CurTime + comp.CheckWait;
            }
        }
    }
    private void GiveInfluence(Entity<CosmicCultistComponent> ent, InfluencePrototype proto)
    {
        if (proto.InfluenceType == "influence-type-active")
            Actions.AddAction(ent, proto.Action);

        else if (proto.InfluenceType == "influence-type-passive")
        {
            if (proto.Add != null)
            {
                foreach (var reg in proto.Add.Values)
                {
                    var compType = reg.Component.GetType();
                    if (HasComp(ent, compType))
                        continue;
                    AddComp(ent, _componentFactory.GetComponent(compType));
                }
            }

            if (proto.Remove != null)
            {
                foreach (var reg in proto.Remove.Values)
                {
                    RemComp(ent, reg.Component.GetType());
                }
            }
        }
        else if (proto.InfluenceType == "influence-type-consumable")
        {
            ent.Comp.AstralAegisStacks += 2;
        }
        _antag.SendBriefing(ent, Loc.GetString(proto.Name), Color.FromHex("#cae8e8"), null);
        _antag.SendBriefing(ent, Loc.GetString(proto.Description), Color.FromHex("#4cabb3"), null);
        Dirty(ent);
    }

    protected override void OnMonumentInteracted(Entity<CosmicMonumentComponent> ent, ref InteractHandEvent args)
    {
        var target = args.User;

        if (args.Handled || !CultQuery.TryComp(target, out var cultComp) || _status.HasStatusEffect(target, SharedStunSystem.StunId))
            return;

        if (cultComp.MonumentVisits <= 0 || cultComp.UnlockedInfluences.Count <= 0)
            return;

        var influenceToGain = Random.PickAndTake(cultComp.UnlockedInfluences);
        if (influenceToGain.Id == "InfluenceAstralAegis")
            cultComp.UnlockedInfluences.Add(influenceToGain, 10); // If we rolled Aegis, add it back to the pool.
        else
            cultComp.OwnedInfluences.Add(influenceToGain);

        if (!ProtoMan.TryIndex(influenceToGain, out var proto))
            return;

        cultComp.MonumentVisits--;
        if (TryComp<CosmicShiftedComponent>(target, out var shiftComp))
        {
            shiftComp.Occupied = true;
            shiftComp.AutoReturnTimer = Timing.CurTime + TimeSpan.FromSeconds(2.5f);
        }

        RaiseNetworkEvent(new InfluenceVisualsEvent(GetNetEntity(target), GetNetEntity(ent.Owner), proto.Icon, cultComp.MonumentGachaSfx));
        _status.TryAddStatusEffectDuration(target, SharedStunSystem.StunId, TimeSpan.FromSeconds(2.5f));

        args.Handled = true;
        GiveInfluence((target, cultComp), proto);
        Dirty(target, cultComp);
    }

    [SubscribeLocalEvent]
    private void OnStartCultist(Entity<CosmicCultistComponent> ent, ref ComponentInit args)
    {
        foreach (var influence in ProtoMan.EnumeratePrototypes<InfluencePrototype>().Where(influence => influence.Tier == 1))
        {
            if (ent.Comp.UnlockedInfluences.ContainsKey(influence))
                continue;
            ent.Comp.UnlockedInfluences.Add(influence, influence.Weight);
        }
        ent.Comp.UnlockedInfluences.Add("InfluenceAstralAegis", 5);
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnStartInfluenceStride(Entity<InfluenceStrideComponent> ent, ref ComponentInit args) // i wish movespeed was easier to work with
    {
        _movementMod.TryUpdateMovementSpeedModDuration(ent, MovementModStatusSystem.StrideSpeedup, null, 1.1f, 1.1f);
    }
}
