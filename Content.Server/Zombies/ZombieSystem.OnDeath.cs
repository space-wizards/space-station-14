using Content.Server.Administration.Commands;
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chat;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Humanoid;
using Content.Server.IdentityManagement;
using Content.Server.Inventory;
using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Server.Speech.Components;
using Content.Server.Temperature.Components;
using Content.Server.Traitor;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Weapons.Melee;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;

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
            var mindcomp = EnsureComp<MindComponent>(target);

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

                _sharedHuApp.SetSkinColor(target, zombie.Settings.SkinColor, verify: false, humanoid: huApComp);
                _sharedHuApp.SetBaseLayerColor(target, HumanoidVisualLayers.Eyes, zombie.Settings.EyeColor, humanoid: huApComp);

                // this might not resync on clone?
                _sharedHuApp.SetBaseLayerId(target, HumanoidVisualLayers.Tail, zombie.Settings.BaseLayerExternal, humanoid: huApComp);
                _sharedHuApp.SetBaseLayerId(target, HumanoidVisualLayers.HeadSide, zombie.Settings.BaseLayerExternal, humanoid: huApComp);
                _sharedHuApp.SetBaseLayerId(target, HumanoidVisualLayers.HeadTop, zombie.Settings.BaseLayerExternal, humanoid: huApComp);
                _sharedHuApp.SetBaseLayerId(target, HumanoidVisualLayers.Snout, zombie.Settings.BaseLayerExternal, humanoid: huApComp);

                melee.Damage = zombie.Settings.AttackDamage;
            }

            //The zombie gets the assigned damage weaknesses and strengths
            _damageable.SetDamageModifierSetId(target, "Zombie");

            //This makes it so the zombie doesn't take bloodloss damage.
            //NOTE: they are supposed to bleed, just not take damage
            _bloodstream.SetBloodLossThreshold(target, 0f);

            //This is specifically here to combat insuls, because frying zombies on grilles is funny as shit.
            _serverInventory.TryUnequip(target, "gloves", true, true);
            //Should prevent instances of zombies using comms for information they shouldnt be able to have.
            _serverInventory.TryUnequip(target, "ears", true, true);

            //popup
            _popup.PopupEntity(Loc.GetString("zombie-transform", ("target", target)), target, PopupType.LargeCaution);

            //Make it sentient if it's an animal or something
            if (!HasComp<InputMoverComponent>(target)) //this component is cursed and fucks shit up
                MakeSentientCommand.MakeSentient(target, EntityManager);

            //Make the zombie not die in the cold. Good for space zombies
            if (TryComp<TemperatureComponent>(target, out var tempComp))
                tempComp.ColdDamage.ClampMax(0);

            // Begin a revive here
            _mobThreshold.SetAllowRevives(target, true);

            //Heals the zombie from all the damage it took while human
            if (TryComp<DamageableComponent>(target, out var damageablecomp))
                _damageable.SetAllDamage(target, damageablecomp, 0);

            // Revive them now
            if (TryComp<MobStateComponent>(target, out var mobstate) && mobstate.CurrentState==MobState.Dead)
                _mobState.ChangeMobState(target, MobState.Alive, mobstate);

            _mobThreshold.SetAllowRevives(target, false);

            //gives it the funny "Zombie ___" name.
            var meta = MetaData(target);
            zombie.BeforeZombifiedEntityName = meta.EntityName;
            meta.EntityName = Loc.GetString("zombie-name-prefix", ("target", meta.EntityName));

            _identity.QueueIdentityUpdate(target);

            if (mindcomp.Mind != null && mindcomp.Mind.TryGetSession(out var session))
            {
                //Zombie role for player manifest
                mindcomp.Mind.AddRole(new ZombieRole(mindcomp.Mind, _proto.Index<AntagPrototype>(zombie.Settings.ZombieRoleId)));
                //Greeting message for new bebe zombers
                _chatMan.DispatchServerMessage(session, Loc.GetString("zombie-infection-greeting"));

                // Notify player about new role assignment
                _audioSystem.PlayGlobal(zombie.Settings.GreetSoundNotification, session);
            }

            if (!HasComp<GhostRoleMobSpawnerComponent>(target) && !mindcomp.HasMind) //this specific component gives build test trouble so pop off, ig
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
                _proto.TryIndex(zombie.Settings.EmoteSoundsId, out zombie.Settings.EmoteSounds);
            }

            // Groaning when damaged
            EnsureComp<EmoteOnDamageComponent>(target);
            _emoteOnDamage.AddEmote(target, "Scream");

            // Random groaning
            EnsureComp<AutoEmoteComponent>(target);
            _autoEmote.AddEmote(target, "ZombieGroan");

            // Make an emote on returning to life
            _chat.TryEmoteWithoutChat(target, "ZombieGroan");

            _passiveHeal.BeginHealing(target, zombie.Settings.HealingPerSec);

            // Goes through every hand, drops the items in it, then removes the hand
            // may become the source of various bugs.
            foreach (var hand in _sharedHands.EnumerateHands(target))
            {
                _sharedHands.SetActiveHand(target, hand);
                _sharedHands.DoDrop(target, hand);
                _sharedHands.RemoveHand(target, hand.Name);
            }
            RemComp<HandsComponent>(target);

            // No longer waiting to become a zombie:
            // Requires deferral one of these are (probably) the cause of the call to ZombifyEntity in the first place.
            RemCompDeferred<PendingZombieComponent>(target);
            RemCompDeferred<InitialInfectedComponent>(target);
            EnsureComp<LivingZombieComponent>(target);

            // zombie gamemode stuff
            RaiseLocalEvent(new EntityZombifiedEvent(target));

            // Zombies get slowdown once they convert
            // See SharedZombieSystem.OnRefreshSpeed
            _movementSpeedModifier.RefreshMovementSpeedModifiers(target);
        }
    }
}
