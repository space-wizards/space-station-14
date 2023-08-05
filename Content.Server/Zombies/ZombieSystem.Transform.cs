using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Chat;
using Content.Server.Chat.Managers;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Humanoid;
using Content.Server.IdentityManagement;
using Content.Server.Inventory;
using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Roles;
using Content.Server.Speech.Components;
using Content.Server.Temperature.Components;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Server.Mind;
using Content.Server.NPC;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Tools.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Zombies;
using Robust.Shared.Audio;

namespace Content.Server.Zombies
{
    /// <summary>
    ///     Handles zombie propagation and inherent zombie traits
    /// </summary>
    /// <remarks>
    ///     Don't Open, Shitcode Inside
    /// </remarks>
    public sealed partial class ZombieSystem
    {
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly ServerInventorySystem _inventory = default!;
        [Dependency] private readonly NpcFactionSystem _faction = default!;
        [Dependency] private readonly NPCSystem _npc = default!;
        [Dependency] private readonly HumanoidAppearanceSystem _sharedHuApp = default!;
        [Dependency] private readonly IdentitySystem _identity = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
        [Dependency] private readonly SharedCombatModeSystem _combat = default!;
        [Dependency] private readonly IChatManager _chatMan = default!;
        [Dependency] private readonly MindSystem _mind = default!;
        [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        /// <summary>
        ///     This is the general purpose function to call if you want to zombify an entity.
        ///     It handles both humanoid and nonhumanoid transformation and everything should be called through it.
        /// </summary>
        /// <param name="target">the entity being zombified</param>
        /// <remarks>
        ///     ALRIGHT BIG BOY. YOU'VE COME TO THE LAYER OF THE BEAST. THIS IS YOUR WARNING.
        ///     This function is the god function for zombie stuff, and it is cursed. I have
        ///     attempted to label everything thoroughly for your sanity. I have attempted to
        ///     rewrite this, but this is how it shall lie eternal. Turn back now.
        ///     -emo
        /// </remarks>
        public void ZombifyEntity(EntityUid target, MobStateComponent? mobState = null, ZombieComponent? zombie = null)
        {
            //Don't zombify living zombies
            if (HasComp<LivingZombieComponent>(target))
                return;

            if (!Resolve(target, ref mobState, logMissing: false))
                return;

            //He's gotta have a mind
            var mindcomp = EnsureComp<MindContainerComponent>(target);

            //If you weren't already, you're a real zombie now, son.
            zombie ??= EnsureComp<ZombieComponent>(target);

            if (zombie.Family.Rules == EntityUid.Invalid)
            {
                // Attempt to find a zombie rule to attach this zombie to
                var (rulesUid, rules) = _zombieRule.FindActiveRule();
                if (rules != null)
                {
                    // Admin command added a new zombie to this existing rule.
                    zombie.Settings = rules.EarlySettings;
                    zombie.VictimSettings = rules.VictimSettings;
                    zombie.Family = new ZombieFamily(){ Rules = rulesUid, Generation = 0 };
                    _zombieRule.AddToInfectedList(target, zombie, rules, mindcomp);
                }
            }

            //we need to basically remove all of these because zombies shouldn't
            //get diseases, breath, be thirst, be hungry, or die in space
            RemComp<RespiratorComponent>(target);
            RemComp<BarotraumaComponent>(target);
            RemComp<HungerComponent>(target);
            RemComp<ThirstComponent>(target);

            //funny voice
            EnsureComp<ReplacementAccentComponent>(target).Accent = "zombie";

            //This is needed for stupid entities that fuck up combat mode component
            //in an attempt to make an entity not attack. This is the easiest way to do it.
            RemComp<CombatModeComponent>(target);
            var combat = AddComp<CombatModeComponent>(target);
            _combat.SetInCombatMode(target, true, combat);

            //This is the actual damage of the zombie. We assign the visual appearance
            //and range here because of stuff we'll find out later
            var melee = EnsureComp<MeleeWeaponComponent>(target);
            melee.ClickAnimation = zombie.Settings.AttackAnimation;
            melee.WideAnimation = zombie.Settings.AttackAnimation;
            melee.Range = zombie.Settings.MeleeRange;
            Dirty(melee);

            //We have specific stuff for humanoid zombies because they matter more
            if (TryComp<HumanoidAppearanceComponent>(target, out var huApComp)) //huapcomp
            {
                //store some values before changing them in case the humanoid get cloned later
                // If somehow a dead zombie gets zombified again, this data might get damaged.
                zombie.BeforeZombifiedSkinColor = huApComp.SkinColor;
                zombie.BeforeZombifiedCustomBaseLayers = new(huApComp.CustomBaseLayers);
                if (TryComp<BloodstreamComponent>(target, out var stream))
                    zombie.BeforeZombifiedBloodReagent = stream.BloodReagent;

                _sharedHuApp.SetSkinColor(target, zombie.Settings.SkinColor, verify: false, humanoid: huApComp);
                _sharedHuApp.SetBaseLayerColor(target, HumanoidVisualLayers.Eyes, zombie.Settings.EyeColor, humanoid: huApComp);

                // this might not resync on clone?
                _sharedHuApp.SetBaseLayerId(target, HumanoidVisualLayers.Tail, zombie.Settings.BaseLayerExternal, humanoid: huApComp);
                _sharedHuApp.SetBaseLayerId(target, HumanoidVisualLayers.HeadSide, zombie.Settings.BaseLayerExternal, humanoid: huApComp);
                _sharedHuApp.SetBaseLayerId(target, HumanoidVisualLayers.HeadTop, zombie.Settings.BaseLayerExternal, humanoid: huApComp);
                _sharedHuApp.SetBaseLayerId(target, HumanoidVisualLayers.Snout, zombie.Settings.BaseLayerExternal, humanoid: huApComp);

                melee.Damage = zombie.Settings.AttackDamage;

                // humanoid zombies get to pry open doors and shit
                var tool = EnsureComp<ToolComponent>(target);
                tool.SpeedModifier = 0.75f;
                tool.Qualities = new ("Prying");
                tool.UseSound = new SoundPathSpecifier("/Audio/Items/crowbar.ogg");
                Dirty(tool);
            }

            Dirty(melee);
            //The zombie gets the assigned damage weaknesses and strengths
            _damageable.SetDamageModifierSetId(target, "Zombie");

            //This makes it so the zombie doesn't take bloodloss damage.
            //NOTE: they are supposed to bleed, just not take damage
            _bloodstream.SetBloodLossThreshold(target, 0f);
            //Give them zombie blood
            _bloodstream.ChangeBloodReagent(target, zombie.Settings.NewBloodReagent);

            //This is specifically here to combat insuls, because frying zombies on grilles is funny as shit.
            _inventory.TryUnequip(target, "gloves", true, true);
            //Should prevent instances of zombies using comms for information they shouldnt be able to have.
            _inventory.TryUnequip(target, "ears", true, true);

            //popup
            _popup.PopupEntity(Loc.GetString("zombie-transform", ("target", target)), target, PopupType.LargeCaution);

            //Make it sentient if it's an animal or something
            MakeSentientCommand.MakeSentient(target, EntityManager);

            //Make the zombie not die in the cold. Good for space zombies
            if (TryComp<TemperatureComponent>(target, out var tempComp))
                tempComp.ColdDamage.ClampMax(0);

            // Begin a revive here
            _mobThreshold.SetAllowRevives(target, true);

            //Heals the zombie from all the damage it took while human
            if (TryComp<DamageableComponent>(target, out var damageablecomp))
                _damageable.SetAllDamage(target, damageablecomp, 0);
            _mobState.ChangeMobState(target, MobState.Alive);

            _mobThreshold.SetAllowRevives(target, false);

            var factionComp = EnsureComp<NpcFactionMemberComponent>(target);
            foreach (var id in new List<string>(factionComp.Factions))
            {
                _faction.RemoveFaction(target, id);
            }
            _faction.AddFaction(target, "Zombie");
            //gives it the funny "Zombie ___" name.
            var meta = MetaData(target);
            zombie.BeforeZombifiedEntityName = meta.EntityName;
            _metaData.SetEntityName(target, Loc.GetString("zombie-name-prefix", ("target", meta.EntityName)), meta);

            _identity.QueueIdentityUpdate(target);

            var mindComp = EnsureComp<MindContainerComponent>(target);
            if (_mind.TryGetMind(target, out var mind, mindComp) && _mind.TryGetSession(mind, out var session))
            {
                //Zombie role for player manifest
                _mind.AddRole(mind, new ZombieRole(mind, _protoManager.Index<AntagPrototype>(zombie.Settings.ZombieRoleId)));

                //Greeting message for new bebe zombers
                _chatMan.DispatchServerMessage(session, Loc.GetString("zombie-infection-greeting"));

                // Notificate player about new role assignment
                _audio.PlayGlobal(zombie.Settings.GreetSoundNotification, session);
            }
            else
            {
                var htn = EnsureComp<HTNComponent>(target);
                htn.RootTask = new HTNCompoundTask() {Task = "SimpleHostileCompound"};
                htn.Blackboard.SetValue(NPCBlackboard.Owner, target);
                _npc.WakeNPC(target, htn);
            }

            if (!HasComp<GhostRoleMobSpawnerComponent>(target) && !mindComp.HasMind) //this specific component gives build test trouble so pop off, ig
            {
                //yet more hardcoding. Visit zombie.ftl for more information.
                var ghostRole = EnsureComp<GhostRoleComponent>(target);
                EnsureComp<GhostTakeoverAvailableComponent>(target);
                ghostRole.RoleName = Loc.GetString(huApComp == null? "zombie-generic-animal": "zombie-generic");
                ghostRole.RoleDescription = Loc.GetString("zombie-role-desc");
                ghostRole.RoleRules = Loc.GetString("zombie-role-rules");
            }

            if (zombie.Settings.EmoteSounds == null && zombie.Settings.EmoteSoundsId != null)
            {
                // If an admin created this zombie, the rule hasn't had a chance to set up the sounds yet. Do that here.
                _protoManager.TryIndex(zombie.Settings.EmoteSoundsId, out zombie.Settings.EmoteSounds);
            }

            // Groaning when damaged
            EnsureComp<EmoteOnDamageComponent>(target);
            _emoteOnDamage.AddEmote(target, "Scream");

            // Random groaning
            EnsureComp<AutoEmoteComponent>(target);
            _autoEmote.AddEmote(target, "ZombieGroan");

            // Make an emote on returning to life
            _chat.TryEmoteWithoutChat(target, "ZombieGroan");

            _passiveHeal.BeginHealing(target, zombie.Settings.HealingPerSec, zombie.Settings.PassiveHealing);

            // Goes through every hand, drops the items in it, then removes the hand
            // may become the source of various bugs.
            if (TryComp<HandsComponent>(target, out var hands))
            {
                foreach (var hand in _hands.EnumerateHands(target))
                {
                    _hands.SetActiveHand(target, hand, hands);
                    _hands.DoDrop(target, hand, handsComp: hands);
                    _hands.RemoveHand(target, hand.Name, hands);
                }

                RemComp(target, hands);
            }

            // No longer waiting to become a zombie:
            // Requires deferral one of these are (probably) the cause of the call to ZombifyEntity in the first place.
            RemCompDeferred<PendingZombieComponent>(target);
            RemCompDeferred<InitialInfectedComponent>(target);
            EnsureComp<LivingZombieComponent>(target);

            // zombie gamemode stuff
            var ev = new EntityZombifiedEvent(target);
            RaiseLocalEvent(target, ref ev, true);
            // Zombies get slowdown once they convert
            // See SharedZombieSystem.OnRefreshSpeed
            _movementSpeedModifier.RefreshMovementSpeedModifiers(target);
        }
    }
}
