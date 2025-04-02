// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Chat.Managers;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Humanoid;
using Content.Server.IdentityManagement;
using Content.Server.Inventory;
using Content.Server.Mind;
using Content.Server.Mind.Commands;
using Content.Server.NPC;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Speech.Components;
using Content.Server.Temperature.Components;
using Content.Shared.CombatMode;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Nutrition.AnimalHusbandry;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Content.Shared.Prying.Components;
using Content.Shared.Traits.Assorted;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Prototypes;
using Robust.Shared.Prototypes;
using System.Numerics;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;
using Content.Server.Zombies;
using Content.Shared.Movement.Components;
using Content.Shared.DeadSpace.Necromorphs.Sanity;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.DeadSpace.NightVision;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.DeadSpace.Necromorphs.Necroobelisk;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead;
using Content.Shared.Mobs.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Rotation;

namespace Content.Server.DeadSpace.Necromorphs.InfectionDead;

public sealed partial class NecromorfSystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ServerInventorySystem _inventory = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedCuffableSystem _cuffs = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedRotationVisualsSystem _sharedRotationVisuals = default!;

    public void Necrofication(EntityUid target, string prototypeId, MobStateComponent? mobState = null)
    {
        if (!_prototypeManager.TryIndex<NecromorfPrototype>(prototypeId, out var necromorf))
            return;

        if (!Resolve(target, ref mobState, logMissing: false))
            return;

        var necromorfComp = AddComp<NecromorfComponent>(target);

        NecromorfLayerComponent necromorfLayercomp = new NecromorfLayerComponent(necromorf.Sprite, necromorf.State, necromorf.IsAnimal);

        if (necromorf.IsAnimal)
        {
            if (TryComp<RotationVisualsComponent>(target, out var rotationVisualsComp))
            {
                rotationVisualsComp.DefaultRotation = Angle.FromDegrees(90);
                _sharedRotationVisuals.ResetHorizontalAngle((target, rotationVisualsComp));
            }
            else
            {
                var newRotationVisualsComp = new RotationVisualsComponent
                {
                    DefaultRotation = Angle.FromDegrees(90)
                };
                AddComp(target, newRotationVisualsComp);
                _sharedRotationVisuals.ResetHorizontalAngle((target, rotationVisualsComp));
            }
        }

        if (!HasComp<NecromorfLayerComponent>(target))
            AddComp(target, necromorfLayercomp);

        RemComp<RespiratorComponent>(target);
        RemComp<BarotraumaComponent>(target);
        RemComp<HungerComponent>(target);
        RemComp<ThirstComponent>(target);
        RemComp<SanityComponent>(target);
        RemComp<ReproductiveComponent>(target);
        RemComp<ReproductivePartnerComponent>(target);
        RemComp<LegsParalyzedComponent>(target);

        if (!HasComp<NightVisionComponent>(target))
            AddComp<NightVisionComponent>(target);

        if (!HasComp<ImmunNecroobeliskComponent>(target))
            AddComp<ImmunNecroobeliskComponent>(target);

        if (!HasComp<ZombieImmuneComponent>(target))
            AddComp<ZombieImmuneComponent>(target);

        if (!HasComp<IgnoreKudzuComponent>(target))
            AddComp<IgnoreKudzuComponent>(target);

        if (HasComp<SlowOnDamageComponent>(target) && !necromorf.IsSlowOnDamage)
            RemComp<SlowOnDamageComponent>(target);

        var accentType = "genericAggressive";

        EnsureComp<ReplacementAccentComponent>(target).Accent = accentType;

        var combat = EnsureComp<CombatModeComponent>(target);
        RemComp<PacifiedComponent>(target);
        _combat.SetCanDisarm(target, false, combat);
        _combat.SetInCombatMode(target, true, combat);

        if (_mobThreshold.TryGetThresholdForState(target, MobState.Dead, out var deadThreshold))
            _mobThreshold.SetMobStateThreshold(target, deadThreshold.Value * necromorf.ThresholdMultiply, MobState.Dead);

        if (_mobThreshold.TryGetThresholdForState(target, MobState.Critical, out var critThreshold))
            _mobThreshold.SetMobStateThreshold(target, critThreshold.Value * necromorf.ThresholdMultiply, MobState.Critical);

        var melee = EnsureComp<MeleeWeaponComponent>(target);
        melee.Animation = necromorfComp.AttackAnimation;
        melee.AltDisarm = false;
        melee.Range = 1.2f;
        melee.Angle = 0.0f;
        melee.HitSound = necromorfComp.BiteSound;

        if (necromorf != null)
        {
            if (necromorf.IsCanUseInventory == false)
            {
                if (_inventory.TryGetSlots(target, out var slotDefinitions))
                {

                    foreach (var slot in slotDefinitions)
                    {
                        _inventory.TryUnequip(target, slot.Name, true, true);
                    }
                }
            }
        }

        if (TryComp<HumanoidAppearanceComponent>(target, out var huApComp)) //huapcomp
        {
            //store some values before changing them in case the humanoid get cloned later
            necromorfComp.BeforeNecroficationSkinColor = huApComp.SkinColor;
            necromorfComp.BeforeNecroficationEyeColor = huApComp.EyeColor;
            necromorfComp.BeforeNecroficationCustomBaseLayers = new(huApComp.CustomBaseLayers);
            if (TryComp<BloodstreamComponent>(target, out var stream))
                necromorfComp.BeforeNecroficationBloodReagent = stream.BloodReagent;

            _humanoidAppearance.SetSkinColor(target, necromorfComp.SkinColor, verify: false, humanoid: huApComp);

            // Messing with the eye layer made it vanish upon cloning, and also it didn't even appear right
            huApComp.EyeColor = necromorfComp.EyeColor;

            if (necromorf != null && necromorf.LayersToHide != null)
            {
                foreach (var layer in necromorf.LayersToHide)
                {
                    _humanoidAppearance.SetLayerVisibility(target, layer, false);
                }
            }

            _inventory.TryUnequip(target, "gloves", true, true);
            _inventory.TryUnequip(target, "ears", true, true);

            if (TryComp<CuffableComponent>(target, out var cuffable))
            {
                if (cuffable.Container.ContainedEntities.Count != 0)
                {
                    var cuffsToRemove = cuffable.LastAddedCuffs;
                    _cuffs.Uncuff(target, target, cuffsToRemove);
                }
            }

            if (TryComp(target, out HandsComponent? hands))
            {
                foreach (var hand in _hands.EnumerateHands(target, hands))
                {
                    _hands.TryDrop(target, hand);
                }
            }

            if (necromorf != null && !string.IsNullOrEmpty(necromorf.Claws))
            {
                _inventory.TryUnequip(target, "neck", true, true);
                var item = Spawn(necromorf.Claws, Transform(target).Coordinates);
                _inventory.TryEquip(target, item, "neck", true, true);
            }

            if (necromorf != null && !string.IsNullOrEmpty(necromorf.Hardsuit))
            {
                _inventory.TryUnequip(target, "outerClothing", true, true);
                var item = Spawn(necromorf.Hardsuit, Transform(target).Coordinates);
                _inventory.TryEquip(target, item, "outerClothing", true, true);
            }

            DamageSpecifier dspec = new()
            {
                DamageDict = new()
                {
                    { "Slash", 13 },
                    { "Piercing", 7 },
                    { "Structural", 10 }
                }
            };

            melee.Damage = necromorf?.Damage ?? dspec;

            var pryComp = EnsureComp<PryingComponent>(target);
            pryComp.SpeedModifier = 0.75f;
            pryComp.PryPowered = true;
            pryComp.Force = true;

            Dirty(target, pryComp);
        }

        if (necromorf != null)
            necromorfComp.IsCanUseInventory = necromorf.IsCanUseInventory;

        Dirty(target, melee);

        if (necromorf != null && necromorf.DamageModifierSet != null)
            _damageable.SetDamageModifierSetId(target, necromorf.DamageModifierSet);

        _bloodstream.SetBloodLossThreshold(target, 0f);

        _bloodstream.ChangeBloodReagent(target, necromorfComp.NewBloodReagent);

        _popup.PopupEntity(Loc.GetString("necro-transform", ("target", target)), target, PopupType.LargeCaution);

        MakeSentientCommand.MakeSentient(target, EntityManager);

        if (necromorf != null)
        {
            necromorfComp.MovementSpeedMultiply = necromorf.MovementSpeedMultiply;
            _movement.RefreshMovementSpeedModifiers(target);
        }

        if (TryComp<TemperatureComponent>(target, out var tempComp))
            tempComp.ColdDamage.ClampMax(0);

        if (TryComp<DamageableComponent>(target, out var damageablecomp))
            _damageable.SetAllDamage(target, damageablecomp, 0);
        _mobState.ChangeMobState(target, MobState.Alive);

        _faction.ClearFactions(target, dirty: false);
        _faction.AddFaction(target, "Necromorfs");

        _identity.QueueIdentityUpdate(target);

        //He's gotta have a mind
        var hasMind = _mind.TryGetMind(target, out var mindId, out _);
        if (hasMind && _mind.TryGetSession(mindId, out var session))
        {
            _chatMan.DispatchServerMessage(session, Loc.GetString("Вы стали некроморфом. Ваша цель — найти живых и попытаться устранить их. Работайте вместе с другими некроморфами."));
        }
        else
        {
            RemComp<HTNComponent>(target);
            var htn = EnsureComp<HTNComponent>(target);
            htn.RootTask = new HTNCompoundTask() { Task = "SimpleHostileCompound" };
            htn.Blackboard.SetValue(NPCBlackboard.Owner, target);
            _npc.WakeNPC(target, htn);
        }

        if (!HasComp<GhostRoleMobSpawnerComponent>(target) && !hasMind) //this specific component gives build test trouble so pop off, ig
        {
            //yet more hardcoding. Visit zombie.ftl for more information.
            var ghostRole = EnsureComp<GhostRoleComponent>(target);
            EnsureComp<GhostTakeoverAvailableComponent>(target);
            ghostRole.RoleName = Loc.GetString("Некроморф");
            ghostRole.RoleDescription = Loc.GetString("Похож на мутировавший труп");
            ghostRole.RoleRules = Loc.GetString("Вы антагонист. Ваша цель — найти живых и попытаться устранить их.");
        }

        if (TryComp<HandsComponent>(target, out var handsComp))
        {
            _hands.RemoveHands(target);
            RemComp(target, handsComp);
        }

        RemComp<PullerComponent>(target);

        if (necromorf != null)
        {
            necromorfComp.MovementSpeedMultiply = necromorf.MovementSpeedMultiply;
        }

        if (necromorf != null && necromorf.Scale != 0)
            SetScale(target, necromorf.Scale);

        if (necromorf != null && necromorf.Components != null)
            EntityManager.AddComponents(target, necromorf.Components);

    }
    private void SetScale(EntityUid uid, float scale)
    {
        var physics = EntityManager.System<SharedPhysicsSystem>();
        var appearance = EntityManager.System<AppearanceSystem>();

        EntityManager.EnsureComponent<ScaleVisualsComponent>(uid);

        var appearanceComponent = EntityManager.EnsureComponent<AppearanceComponent>(uid);
        if (!appearance.TryGetData<Vector2>(uid, ScaleVisuals.Scale, out var oldScale, appearanceComponent))
            oldScale = Vector2.One;

        appearance.SetData(uid, ScaleVisuals.Scale, oldScale * scale, appearanceComponent);

        if (EntityManager.TryGetComponent(uid, out FixturesComponent? manager))
        {
            foreach (var (id, fixture) in manager.Fixtures)
            {
                switch (fixture.Shape)
                {
                    case PhysShapeCircle circle:
                        physics.SetPositionRadius(uid, id, fixture, circle, circle.Position * scale, circle.Radius * scale, manager);
                        break;
                    case PolygonShape poly:
                        var verts = poly.Vertices;

                        for (var i = 0; i < poly.VertexCount; i++)
                        {
                            verts[i] *= scale;
                        }

                        physics.SetVertices(uid, id, fixture, poly, verts, manager);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
}
