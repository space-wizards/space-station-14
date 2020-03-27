///////////	cleric's den items.

//Primary reward: the cleric's mace design disk.
/obj/item/disk/design_disk/adv/cleric_mace
	name = "Enshrined Disc of Smiting"

/obj/item/disk/design_disk/adv/cleric_mace/Initialize()
	. = ..()
	var/datum/design/cleric_mace/M = new
	blueprints[1] = M

/obj/item/paper/fluff/ruins/clericsden/contact
	info = "Father Aurellion, the ritual is complete, and soon our brothers at the bastion will see the error of our ways. After all, a god of clockwork or blood? Preposterous. Only the TRUE GOD should have so much power. Signed, Father Odivallus."

/obj/item/paper/fluff/ruins/clericsden/warning
	info = "FATHER ODIVALLUS DO NOT GO FORWARD WITH THE RITUAL. THE ASTEROID WE'RE ANCHORED TO IS UNSTABLE, YOU WILL DESTROY THE STATION. I HOPE THIS REACHES YOU IN TIME. FATHER AURELLION."

/mob/living/simple_animal/hostile/construct/proteon
	name = "Proteon"
	real_name = "Proteon"
	desc = "A weaker construct meant to scour ruins for objects of Nar'Sie's affection. Those barbed claws are no joke."
	icon_state = "proteon"
	icon_living = "proteon"
	maxHealth = 35
	health = 35
	melee_damage_lower = 8
	melee_damage_upper = 10
	retreat_distance = 4 //AI proteons will rapidly move in and out of combat to avoid conflict, but will still target and follow you.
	attack_verb_continuous = "pinches"
	attack_verb_simple = "pinch"
	environment_smash = ENVIRONMENT_SMASH_WALLS
	attack_sound = 'sound/weapons/punch2.ogg'
	playstyle_string = "<b>You are a Proteon. Your abilities in combat are outmatched by most combat constructs, but you are still fast and nimble. Run metal and supplies, and cooperate with your fellow cultists.</b>"

/mob/living/simple_animal/hostile/construct/proteon/hostile //Style of mob spawned by trapped cult runes in the cleric ruin.
	AIStatus = AI_ON
	environment_smash = ENVIRONMENT_SMASH_STRUCTURES //standard ai construct behavior, breaks things if it wants, but not walls.
