#define REGENERATION_DELAY 60  // After taking damage, how long it takes for automatic regeneration to begin for megacarps (ty robustin!)

/mob/living/simple_animal/hostile/carp
	name = "space carp"
	desc = "A ferocious, fang-bearing creature that resembles a fish."
	icon = 'icons/mob/carp.dmi'
	icon_state = "base"
	icon_living = "base"
	icon_dead = "base_dead"
	icon_gib = "carp_gib"
	mob_biotypes = MOB_ORGANIC|MOB_BEAST
	speak_chance = 0
	turns_per_move = 5
	butcher_results = list(/obj/item/reagent_containers/food/snacks/carpmeat = 2)
	response_help_continuous = "pets"
	response_help_simple = "pet"
	response_disarm_continuous = "gently pushes aside"
	response_disarm_simple = "gently push aside"
	emote_taunt = list("gnashes")
	taunt_chance = 30
	speed = 0
	maxHealth = 25
	health = 25
	spacewalk = TRUE
	search_objects = 1
	wanted_objects = list(/obj/item/storage/cans)

	harm_intent_damage = 8
	obj_damage = 50
	melee_damage_lower = 20
	melee_damage_upper = 20
	attack_verb_continuous = "bites"
	attack_verb_simple = "bite"
	attack_sound = 'sound/weapons/bite.ogg'
	speak_emote = list("gnashes")

	//Space carp aren't affected by cold.
	atmos_requirements = list("min_oxy" = 0, "max_oxy" = 0, "min_tox" = 0, "max_tox" = 0, "min_co2" = 0, "max_co2" = 0, "min_n2" = 0, "max_n2" = 0)
	minbodytemp = 0
	maxbodytemp = 1500
	faction = list("carp")
	movement_type = FLYING
	pressure_resistance = 200
	gold_core_spawnable = HOSTILE_SPAWN

	var/random_color = TRUE //if the carp uses random coloring
	var/rarechance = 1 //chance for rare color variant
	var/snack_distance = 0

	var/static/list/carp_colors = list(\
	"lightpurple" = "#c3b9f1", \
	"lightpink" = "#da77a8", \
	"green" = "#70ff25", \
	"grape" = "#df0afb", \
	"swamp" = "#e5e75a", \
	"turquoise" = "#04e1ed", \
	"brown" = "#ca805a", \
	"teal" = "#20e28e", \
	"lightblue" = "#4d88cc", \
	"rusty" = "#dd5f34", \
	"beige" = "#bbaeaf", \
	"yellow" = "#f3ca4a", \
	"blue" = "#09bae1", \
	"palegreen" = "#7ef099", \
	)
	var/static/list/carp_colors_rare = list(\
	"silver" = "#fdfbf3", \
	)

/mob/living/simple_animal/hostile/carp/Initialize(mapload)
	. = ..()
	carp_randomify(rarechance)
	update_icons()

/mob/living/simple_animal/hostile/carp/proc/carp_randomify(rarechance)
	if(random_color)
		var/our_color
		if(prob(rarechance))
			our_color = pick(carp_colors_rare)
			add_atom_colour(carp_colors_rare[our_color], FIXED_COLOUR_PRIORITY)
		else
			our_color = pick(carp_colors)
			add_atom_colour(carp_colors[our_color], FIXED_COLOUR_PRIORITY)
		add_carp_overlay()

/mob/living/simple_animal/hostile/carp/proc/add_carp_overlay()
	if(!random_color)
		return
	cut_overlays()
	var/mutable_appearance/base_overlay = mutable_appearance(icon, "base_mouth")
	base_overlay.appearance_flags = RESET_COLOR
	add_overlay(base_overlay)

/mob/living/simple_animal/hostile/carp/proc/add_dead_carp_overlay()
	if(!random_color)
		return
	cut_overlays()
	var/mutable_appearance/base_dead_overlay = mutable_appearance(icon, "base_dead_mouth")
	base_dead_overlay.appearance_flags = RESET_COLOR
	add_overlay(base_dead_overlay)

/mob/living/simple_animal/hostile/carp/proc/chomp_plastic()
	var/obj/item/storage/cans/tasty_plastic = locate(/obj/item/storage/cans) in oview(src, 9)
	if(tasty_plastic)
		snack_distance = get_dist(src.loc,tasty_plastic.loc)
		if(snack_distance <= 1)
			src.visible_message("<span class='notice'>[src] gets its head stuck in [tasty_plastic], and gets cut breaking free from it!</span>", "<span class='notice'>You try to avoid [tasty_plastic], but it looks so... delicious... Ow! It cuts the inside of your mouth!</span>")
			new /obj/effect/decal/cleanable/plastic(src.loc)
			adjustBruteLoss(5)
			qdel(tasty_plastic)

/mob/living/simple_animal/hostile/carp/Life()
	. = ..()
	if(stat == CONSCIOUS)
		chomp_plastic()

/mob/living/simple_animal/hostile/carp/death(gibbed)
	. = ..()
	cut_overlays()
	if(!random_color || gibbed)
		return
	add_dead_carp_overlay()

/mob/living/simple_animal/hostile/carp/revive(full_heal = FALSE, admin_revive = FALSE)
	. = ..()
	if(.)
		regenerate_icons()

/mob/living/simple_animal/hostile/carp/regenerate_icons()
	cut_overlays()
	if(!random_color)
		return
	if(stat != DEAD)
		add_carp_overlay()
	else
		add_dead_carp_overlay()
	..()

/mob/living/simple_animal/hostile/carp/holocarp
	icon_state = "holocarp"
	icon_living = "holocarp"
	maxbodytemp = INFINITY
	gold_core_spawnable = NO_SPAWN
	del_on_death = 1
	random_color = FALSE

/mob/living/simple_animal/hostile/carp/megacarp
	icon = 'icons/mob/broadMobs.dmi'
	name = "Mega Space Carp"
	desc = "A ferocious, fang bearing creature that resembles a shark. This one seems especially ticked off."
	icon_state = "megacarp"
	icon_living = "megacarp"
	icon_dead = "megacarp_dead"
	icon_gib = "megacarp_gib"
	maxHealth = 20
	health = 20
	pixel_x = -16
	mob_size = MOB_SIZE_LARGE
	random_color = FALSE

	obj_damage = 80
	melee_damage_lower = 20
	melee_damage_upper = 20

	var/regen_cooldown = 0
	var/rideable = FALSE

/mob/living/simple_animal/hostile/carp/megacarp/Initialize()
	. = ..()
	name = "[pick(GLOB.megacarp_first_names)] [pick(GLOB.megacarp_last_names)]"
	melee_damage_lower += rand(2, 10)
	melee_damage_upper += rand(10,20)
	maxHealth += rand(30,60)
	move_to_delay = rand(3,7)

/mob/living/simple_animal/hostile/carp/megacarp/adjustHealth(amount, updating_health = TRUE, forced = FALSE)
	. = ..()
	if(.)
		regen_cooldown = world.time + REGENERATION_DELAY

/mob/living/simple_animal/hostile/carp/megacarp/Life()
	. = ..()
	if(regen_cooldown < world.time)
		heal_overall_damage(4)
	if(!rideable && src.mind)
		can_buckle = TRUE
		buckle_lying = FALSE
		var/datum/component/riding/D = LoadComponent(/datum/component/riding)
		D.set_riding_offsets(RIDING_OFFSET_ALL, list(TEXT_NORTH = list(1, 8), TEXT_SOUTH = list(1, 8), TEXT_EAST = list(-3, 6), TEXT_WEST = list(3, 6)))
		D.set_vehicle_dir_offsets(SOUTH, pixel_x, 0)
		D.set_vehicle_dir_offsets(NORTH, pixel_x, 0)
		D.set_vehicle_dir_offsets(EAST, pixel_x, 0)
		D.set_vehicle_dir_offsets(WEST, pixel_x, 0)
		D.set_vehicle_dir_layer(SOUTH, ABOVE_MOB_LAYER)
		D.set_vehicle_dir_layer(NORTH, OBJ_LAYER)
		D.set_vehicle_dir_layer(EAST, OBJ_LAYER)
		D.set_vehicle_dir_layer(WEST, OBJ_LAYER)
		rideable = TRUE

/mob/living/simple_animal/hostile/carp/cayenne
	name = "Cayenne"
	desc = "A failed Syndicate experiment in weaponized space carp technology, it now serves as a lovable mascot."
	gender = FEMALE
	speak_emote = list("squeaks")
	gold_core_spawnable = NO_SPAWN
	faction = list(ROLE_SYNDICATE)
	AIStatus = AI_OFF
	rarechance = 10

#undef REGENERATION_DELAY
