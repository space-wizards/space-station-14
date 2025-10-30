using Content.Server.Administration.Managers;
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Chat;
using Content.Server.Chat.Managers;
using Content.Server.Ghost;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Humanoid;
using Content.Server.Inventory;
using Content.Server.Mind;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.StationEvents.Components;
using Content.Server.Speech.Components;
using Content.Shared.Body.Components;
using Content.Shared.CombatMode;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.NPC.Systems;
using Content.Shared.Nutrition.AnimalHusbandry;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Content.Shared.Zombies;
using Content.Shared.Prying.Components;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Audio.Systems;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Tag;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Temperature.Components;

namespace Content.Server.Zombies;

/// <summary>
///     Handles zombie propagation and inherent zombie traits
/// </summary>
/// <remarks>
///     Don't Shitcode Open Inside
/// </remarks>
public sealed partial class ZombieSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IBanManager _ban = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    [Dependency] private readonly ServerInventorySystem _inventory = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly NameModifierSystem _nameMod = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    private static readonly ProtoId<TagPrototype> InvalidForGlobalSpawnSpellTag = "InvalidForGlobalSpawnSpell";
    private static readonly ProtoId<TagPrototype> CannotSuicideTag = "CannotSuicide";
    private static readonly ProtoId<NpcFactionPrototype> ZombieFaction = "Zombie";
    private static readonly string MindRoleZombie = "MindRoleZombie";
    private static readonly List<ProtoId<AntagPrototype>> BannableZombiePrototypes = ["Zombie"];

    /// <summary>
    /// Handles an entity turning into a zombie when they die or go into crit
    /// </summary>
    private void OnDamageChanged(EntityUid uid, ZombifyOnDeathComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            ZombifyEntity(uid, args.Component);
        }
    }

    /// <summary>
    ///     This is the general purpose function to call if you want to zombify an entity.
    ///     It handles both humanoid and nonhumanoid transformation and everything should be called through it.
    /// </summary>
    /// <param name="target">the entity being zombified</param>
    /// <param name="mobState"></param>
    /// <remarks>
    ///     ALRIGHT BIG BOYS, GIRLS AND ANYONE ELSE. YOU'VE COME TO THE LAYER OF THE BEAST. THIS IS YOUR WARNING.
    ///     This function is the god function for zombie stuff, and it is cursed. I have
    ///     attempted to label everything thouroughly for your sanity. I have attempted to
    ///     rewrite this, but this is how it shall lie eternal. Turn back now.
    ///     -emo
    /// </remarks>
    public void ZombifyEntity(EntityUid target, MobStateComponent? mobState = null)
    {
        //Don't zombfiy zombies
        if (HasComp<ZombieComponent>(target) || HasComp<ZombieImmuneComponent>(target))
            return;

        if (!Resolve(target, ref mobState, logMissing: false))
            return;

        // Detach role-banned players before zombification
        if (TryComp<ActorComponent>(target, out var actor) && _ban.IsRoleBanned(actor.PlayerSession, BannableZombiePrototypes))
        {
            var sess = actor.PlayerSession;
            var message = Loc.GetString("zombie-roleban-ghosted");

            if (_mind.TryGetMind(sess, out var playerMindEnt, out var playerMind))
            {
                // Detach
                _ghost.SpawnGhost((playerMindEnt, playerMind), target);

                // Notify
                _chatMan.DispatchServerMessage(sess, message);
            }
            else
                Log.Error($"Mind for session '{sess}' could not be found");
        }

        //you're a real zombie now, son.
        var zombiecomp = AddComp<ZombieComponent>(target);

        //we need to basically remove all of these because zombies shouldn't
        //get diseases, breath, be thirst, be hungry, die in space, get double sentience, have offspring or be paraplegic.
        RemComp<RespiratorComponent>(target);
        RemComp<BarotraumaComponent>(target);
        RemComp<HungerComponent>(target);
        RemComp<ThirstComponent>(target);
        RemComp<ReproductiveComponent>(target);
        RemComp<ReproductivePartnerComponent>(target);
        RemComp<LegsParalyzedComponent>(target);
        RemComp<ComplexInteractionComponent>(target);
        RemComp<SentienceTargetComponent>(target);

        //funny voice
        var accentType = "zombie";
        if (TryComp<ZombieAccentOverrideComponent>(target, out var accent))
            accentType = accent.Accent;

        EnsureComp<ReplacementAccentComponent>(target).Accent = accentType;

        //This is needed for stupid entities that fuck up combat mode component
        //in an attempt to make an entity not attack. This is the easiest way to do it.
        var combat = EnsureComp<CombatModeComponent>(target);
        RemComp<PacifiedComponent>(target);
        _combat.SetCanDisarm(target, false, combat);

        //This is the actual damage of the zombie. We assign the visual appearance
        //and range here because of stuff we'll find out later
        var melee = EnsureComp<MeleeWeaponComponent>(target);
        melee.Animation = zombiecomp.AttackAnimation;
        melee.WideAnimation = zombiecomp.AttackAnimation;
        melee.AltDisarm = false;
        melee.Range = 1.2f;
        melee.Angle = 0.0f;
        melee.HitSound = zombiecomp.BiteSound;

        DirtyFields(target, melee, null, fields:
        [
            nameof(MeleeWeaponComponent.Animation),
            nameof(MeleeWeaponComponent.WideAnimation),
            nameof(MeleeWeaponComponent.AltDisarm),
            nameof(MeleeWeaponComponent.Range),
            nameof(MeleeWeaponComponent.Angle),
            nameof(MeleeWeaponComponent.HitSound),
        ]);

        if (mobState.CurrentState == MobState.Alive)
        {
            // Groaning when damaged
            EnsureComp<EmoteOnDamageComponent>(target);
            _emoteOnDamage.AddEmote(target, "Scream");

            // Random groaning
            EnsureComp<AutoEmoteComponent>(target);
            _autoEmote.AddEmote(target, "ZombieGroan");
        }

        //We have specific stuff for humanoid zombies because they matter more
        if (TryComp<HumanoidAppearanceComponent>(target, out var huApComp)) //huapcomp
        {
            //store some values before changing them in case the humanoid get cloned later
            zombiecomp.BeforeZombifiedSkinColor = huApComp.SkinColor;
            zombiecomp.BeforeZombifiedEyeColor = huApComp.EyeColor;
            zombiecomp.BeforeZombifiedCustomBaseLayers = new(huApComp.CustomBaseLayers);
            if (TryComp<BloodstreamComponent>(target, out var stream))
                zombiecomp.BeforeZombifiedBloodReagent = stream.BloodReagent;

            _humanoidAppearance.SetSkinColor(target, zombiecomp.SkinColor, verify: false, humanoid: huApComp);

            // Messing with the eye layer made it vanish upon cloning, and also it didn't even appear right
            huApComp.EyeColor = zombiecomp.EyeColor;

            // this might not resync on clone?
            _humanoidAppearance.SetBaseLayerId(target, HumanoidVisualLayers.Tail, zombiecomp.BaseLayerExternal, humanoid: huApComp);
            _humanoidAppearance.SetBaseLayerId(target, HumanoidVisualLayers.HeadSide, zombiecomp.BaseLayerExternal, humanoid: huApComp);
            _humanoidAppearance.SetBaseLayerId(target, HumanoidVisualLayers.HeadTop, zombiecomp.BaseLayerExternal, humanoid: huApComp);
            _humanoidAppearance.SetBaseLayerId(target, HumanoidVisualLayers.Snout, zombiecomp.BaseLayerExternal, humanoid: huApComp);

            //This is done here because non-humanoids shouldn't get baller damage
            melee.Damage = zombiecomp.DamageOnBite;

            // humanoid zombies get to pry open doors and shit
            var pryComp = EnsureComp<PryingComponent>(target);
            pryComp.SpeedModifier = 0.75f;
            pryComp.PryPowered = true;
            pryComp.Force = true;

            Dirty(target, pryComp);
        }

        Dirty(target, melee);

        //The zombie gets the assigned damage weaknesses and strengths
        _damageable.SetDamageModifierSetId(target, "Zombie");

        //This makes it so the zombie doesn't take bloodloss damage.
        //NOTE: they are supposed to bleed, just not take damage
        _bloodstream.SetBloodLossThreshold(target, 0f);
        //Give them zombie blood
        _bloodstream.ChangeBloodReagent(target, zombiecomp.NewBloodReagent);

        //This is specifically here to combat insuls, because frying zombies on grilles is funny as shit.
        _inventory.TryUnequip(target, "gloves", true, true);
        //Should prevent instances of zombies using comms for information they shouldnt be able to have.
        _inventory.TryUnequip(target, "ears", true, true);

        //popup
        _popup.PopupEntity(Loc.GetString("zombie-transform", ("target", target)), target, PopupType.LargeCaution);

        //Make it sentient if it's an animal or something
        _mind.MakeSentient(target);

        //Make the zombie not die in the cold. Good for space zombies
        if (TryComp<TemperatureComponent>(target, out var tempComp))
            tempComp.ColdDamage.ClampMax(0);

        //Heals the zombie from all the damage it took while human
        _damageable.ClearAllDamage(target);
        _mobState.ChangeMobState(target, MobState.Alive);

        _faction.ClearFactions(target, dirty: false);
        _faction.AddFaction(target, ZombieFaction);

        //gives it the funny "Zombie ___" name.
        _nameMod.RefreshNameModifiers(target);

        _identity.QueueIdentityUpdate(target);

        var htn = EnsureComp<HTNComponent>(target);
        htn.RootTask = new HTNCompoundTask() { Task = "SimpleHostileCompound" };
        htn.Blackboard.SetValue(NPCBlackboard.Owner, target);
        _npc.SleepNPC(target, htn);

        //He's gotta have a mind
        var hasMind = _mind.TryGetMind(target, out var mindId, out var mind);
        if (hasMind && mind != null && _player.TryGetSessionById(mind.UserId, out var session))
        {
            //Zombie role for player manifest
            _role.MindAddRole(mindId, MindRoleZombie, mind: null, silent: true);

            //Greeting message for new bebe zombers
            _chatMan.DispatchServerMessage(session, Loc.GetString("zombie-infection-greeting"));

            // Notificate player about new role assignment
            _audio.PlayGlobal(zombiecomp.GreetSoundNotification, session);
        }
        else
        {
            _npc.WakeNPC(target, htn);
        }

        if (!HasComp<GhostRoleMobSpawnerComponent>(target) && !hasMind) //this specific component gives build test trouble so pop off, ig
        {
            //yet more hardcoding. Visit zombie.ftl for more information.
            var ghostRole = EnsureComp<GhostRoleComponent>(target);
            EnsureComp<GhostTakeoverAvailableComponent>(target);
            ghostRole.RoleName = Loc.GetString("zombie-generic");
            ghostRole.RoleDescription = Loc.GetString("zombie-role-desc");
            ghostRole.RoleRules = Loc.GetString("zombie-role-rules");
            ghostRole.MindRoles.Add(MindRoleZombie);
        }

        if (TryComp<HandsComponent>(target, out var handsComp))
        {
            _hands.RemoveHands(target);
            RemComp(target, handsComp);
        }

        // Sloth: What the fuck?
        // How long until compregistry lmao.
        RemComp<PullerComponent>(target);

        // No longer waiting to become a zombie:
        // Requires deferral because this is (probably) the event which called ZombifyEntity in the first place.
        RemCompDeferred<PendingZombieComponent>(target);

        //zombie gamemode stuff
        var ev = new EntityZombifiedEvent(target);
        RaiseLocalEvent(target, ref ev, true);
        //zombies get slowdown once they convert
        _movementSpeedModifier.RefreshMovementSpeedModifiers(target);

        //Need to prevent them from getting an item, they have no hands.
        // Also prevents them from becoming a Survivor. They're undead.
        _tag.AddTag(target, InvalidForGlobalSpawnSpellTag);
        _tag.AddTag(target, CannotSuicideTag);
    }
}
