using Content.Shared.Roles;
using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
        /// <summary>
        ///     Scales the damage dealt by all melee attacks in the game.
        /// </summary>
        public static readonly CVarDef<float> PlaytestMeleeDamageModifier =
            CVarDef.Create("playtest.melee_damage_modifier", 1f, CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        ///     Scales the damage dealt by all projectiles in the game.
        /// </summary>
        public static readonly CVarDef<float> PlaytestProjectileDamageModifier =
            CVarDef.Create("playtest.projectile_damage_modifier", 1f, CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        ///     Scales the damage dealt by all hitscan attacks in the game.
        /// </summary>
        public static readonly CVarDef<float> PlaytestHitscanDamageModifier =
            CVarDef.Create("playtest.hitscan_damage_modifier", 1f, CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        ///     Scales the damage dealt by all thrown weapons in the game.
        /// </summary>
        public static readonly CVarDef<float> PlaytestThrownDamageModifier =
            CVarDef.Create("playtest.thrown_damage_modifier", 1f, CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        ///     Scales the healing given by all topicals in the game.
        /// </summary>
        public static readonly CVarDef<float> PlaytestTopicalsHealModifier =
            CVarDef.Create("playtest.topicals_heal_modifier", 1f, CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        ///     Scales the damage dealt by all reagents in the game.
        /// </summary>
        public static readonly CVarDef<float> PlaytestReagentDamageModifier =
            CVarDef.Create("playtest.reagent_damage_modifier", 1f, CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        ///     Scales the healing given by all reagents in the game.
        /// </summary>
        public static readonly CVarDef<float> PlaytestReagentHealModifier =
            CVarDef.Create("playtest.reagent_heal_modifier", 1f, CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        ///     Scales the explosion damage dealt in the game.
        /// </summary>
        public static readonly CVarDef<float> PlaytestExplosionDamageModifier =
            CVarDef.Create("playtest.explosion_damage_modifier", 1f, CVar.SERVER | CVar.REPLICATED);

}
