/obj/item/banner
	name = "banner"
	desc = "A banner with Nanotrasen's logo on it."
	icon = 'icons/obj/banner.dmi'
	icon_state = "banner"
	item_state = "banner"
	force = 8
	attack_verb = list("forcefully inspired", "violently encouraged", "relentlessly galvanized")
	lefthand_file = 'icons/mob/inhands/equipment/banners_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/equipment/banners_righthand.dmi'
	var/inspiration_available = TRUE //If this banner can be used to inspire crew
	var/morale_time = 0
	var/morale_cooldown = 600 //How many deciseconds between uses
	var/list/job_loyalties //Mobs with any of these assigned roles will be inspired
	var/list/role_loyalties //Mobs with any of these special roles will be inspired
	var/warcry

/obj/item/banner/examine(mob/user)
	. = ..()
	if(inspiration_available)
		. += "<span class='notice'>Activate it in your hand to inspire nearby allies of this banner's allegiance!</span>"

/obj/item/banner/attack_self(mob/living/carbon/human/user)
	if(!inspiration_available)
		return
	if(morale_time > world.time)
		to_chat(user, "<span class='warning'>You aren't feeling inspired enough to flourish [src] again yet.</span>")
		return
	user.visible_message("<span class='big notice'>[user] flourishes [src]!</span>", \
	"<span class='notice'>You raise [src] skywards, inspiring your allies!</span>")
	playsound(src, "rustle", 100, FALSE)
	if(warcry)
		user.say("[warcry]", forced="banner")
	var/old_transform = user.transform
	user.transform *= 1.2
	animate(user, transform = old_transform, time = 10)
	morale_time = world.time + morale_cooldown

	var/list/inspired = list()
	var/has_job_loyalties = LAZYLEN(job_loyalties)
	var/has_role_loyalties = LAZYLEN(role_loyalties)
	inspired += user //The user is always inspired, regardless of loyalties
	for(var/mob/living/carbon/human/H in range(4, get_turf(src)))
		if(H.stat == DEAD || H == user)
			continue
		if(H.mind && (has_job_loyalties || has_role_loyalties))
			if(has_job_loyalties && (H.mind.assigned_role in job_loyalties))
				inspired += H
			else if(has_role_loyalties && (H.mind.special_role in role_loyalties))
				inspired += H
		else if(check_inspiration(H))
			inspired += H

	for(var/V in inspired)
		var/mob/living/carbon/human/H = V
		if(H != user)
			to_chat(H, "<span class='notice'>Your confidence surges as [user] flourishes [user.p_their()] [name]!</span>")
		inspiration(H)
		special_inspiration(H)

/obj/item/banner/proc/check_inspiration(mob/living/carbon/human/H) //Banner-specific conditions for being eligible
	return

/obj/item/banner/proc/inspiration(mob/living/carbon/human/H)
	H.adjustBruteLoss(-15)
	H.adjustFireLoss(-15)
	H.AdjustStun(-40)
	H.AdjustKnockdown(-40)
	H.AdjustImmobilized(-40)
	H.AdjustParalyzed(-40)
	H.AdjustUnconscious(-40)
	playsound(H, 'sound/magic/staff_healing.ogg', 25, FALSE)

/obj/item/banner/proc/special_inspiration(mob/living/carbon/human/H) //Any banner-specific inspiration effects go here
	return

/obj/item/banner/security
	name = "securistan banner"
	desc = "The banner of Securistan, ruling the station with an iron fist."
	icon_state = "banner_security"
	item_state = "banner_security"
	lefthand_file = 'icons/mob/inhands/equipment/banners_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/equipment/banners_righthand.dmi'
	job_loyalties = list("Security Officer", "Warden", "Detective", "Head of Security")
	warcry = "EVERYONE DOWN ON THE GROUND!!"

/obj/item/banner/security/mundane
	inspiration_available = FALSE

/datum/crafting_recipe/security_banner
	name = "Securistan Banner"
	result = /obj/item/banner/security/mundane
	time = 40
	reqs = list(/obj/item/stack/rods = 2,
				/obj/item/clothing/under/rank/security/officer = 1)
	category = CAT_MISC

/obj/item/banner/medical
	name = "meditopia banner"
	desc = "The banner of Meditopia, generous benefactors that cure wounds and shelter the weak."
	icon_state = "banner_medical"
	item_state = "banner_medical"
	lefthand_file = 'icons/mob/inhands/equipment/banners_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/equipment/banners_righthand.dmi'
	job_loyalties = list("Medical Doctor", "Chemist", "Virologist", "Chief Medical Officer")
	warcry = "No wounds cannot be healed!"

/obj/item/banner/medical/mundane
	inspiration_available = FALSE

/obj/item/banner/medical/check_inspiration(mob/living/carbon/human/H)
	return H.stat //Meditopia is moved to help those in need

/datum/crafting_recipe/medical_banner
	name = "Meditopia Banner"
	result = /obj/item/banner/medical/mundane
	time = 40
	reqs = list(/obj/item/stack/rods = 2,
				/obj/item/clothing/under/rank/medical = 1)
	category = CAT_MISC

/obj/item/banner/medical/special_inspiration(mob/living/carbon/human/H)
	H.adjustToxLoss(-15)
	H.setOxyLoss(0)
	H.reagents.add_reagent(/datum/reagent/medicine/inaprovaline, 5)

/obj/item/banner/science
	name = "sciencia banner"
	desc = "The banner of Sciencia, bold and daring thaumaturges and researchers that take the path less traveled."
	icon_state = "banner_science"
	item_state = "banner_science"
	lefthand_file = 'icons/mob/inhands/equipment/banners_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/equipment/banners_righthand.dmi'
	job_loyalties = list("Scientist", "Roboticist", "Research Director", "Geneticist",)
	warcry = "For Cuban Pete!"

/obj/item/banner/science/mundane
	inspiration_available = FALSE

/obj/item/banner/science/check_inspiration(mob/living/carbon/human/H)
	return H.on_fire //Sciencia is pleased by dedication to the art of Toxins

/datum/crafting_recipe/science_banner
	name = "Sciencia Banner"
	result = /obj/item/banner/science/mundane
	time = 40
	reqs = list(/obj/item/stack/rods = 2,
				/obj/item/clothing/under/rank/rnd/scientist = 1)
	category = CAT_MISC

/obj/item/banner/cargo
	name = "cargonia banner"
	desc = "The banner of the eternal Cargonia, with the mystical power of conjuring any object into existence."
	icon_state = "banner_cargo"
	item_state = "banner_cargo"
	lefthand_file = 'icons/mob/inhands/equipment/banners_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/equipment/banners_righthand.dmi'
	job_loyalties = list("Cargo Technician", "Shaft Miner", "Quartermaster")
	warcry = "Hail Cargonia!"

/obj/item/banner/cargo/mundane
	inspiration_available = FALSE

/datum/crafting_recipe/cargo_banner
	name = "Cargonia Banner"
	result = /obj/item/banner/cargo/mundane
	time = 40
	reqs = list(/obj/item/stack/rods = 2,
				/obj/item/clothing/under/rank/cargo/tech = 1)
	category = CAT_MISC

/obj/item/banner/engineering
	name = "engitopia banner"
	desc = "The banner of Engitopia, wielders of limitless power."
	icon_state = "banner_engineering"
	item_state = "banner_engineering"
	lefthand_file = 'icons/mob/inhands/equipment/banners_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/equipment/banners_righthand.dmi'
	job_loyalties = list("Station Engineer", "Atmospheric Technician", "Chief Engineer")
	warcry = "All hail lord Singuloth!!"

/obj/item/banner/engineering/mundane
	inspiration_available = FALSE

/obj/item/banner/engineering/special_inspiration(mob/living/carbon/human/H)
	H.radiation = 0

/datum/crafting_recipe/engineering_banner
	name = "Engitopia Banner"
	result = /obj/item/banner/engineering/mundane
	time = 40
	reqs = list(/obj/item/stack/rods = 2,
				/obj/item/clothing/under/rank/engineering/engineer = 1)
	category = CAT_MISC

/obj/item/banner/command
	name = "command banner"
	desc = "The banner of Command, a staunch and ancient line of bueraucratic kings and queens."
	//No icon state here since the default one is the NT banner
	job_loyalties = list("Captain", "Head of Personnel", "Chief Engineer", "Head of Security", "Research Director", "Chief Medical Officer")
	warcry = "Hail Nanotrasen!"

/obj/item/banner/command/mundane
	inspiration_available = FALSE

/obj/item/banner/command/check_inspiration(mob/living/carbon/human/H)
	return HAS_TRAIT(H, TRAIT_MINDSHIELD) //Command is stalwart but rewards their allies.

/datum/crafting_recipe/command_banner
	name = "Command Banner"
	result = /obj/item/banner/command/mundane
	time = 40
	reqs = list(/obj/item/stack/rods = 2,
				/obj/item/clothing/under/rank/captain/parade = 1)
	category = CAT_MISC

/obj/item/banner/red
	name = "red banner"
	icon_state = "banner-red"
	item_state = "banner-red"
	desc = "A banner with the logo of the red deity."

/obj/item/banner/blue
	name = "blue banner"
	icon_state = "banner-blue"
	item_state = "banner-blue"
	desc = "A banner with the logo of the blue deity."

/obj/item/storage/backpack/bannerpack
	name = "nanotrasen banner backpack"
	desc = "It's a backpack with lots of extra room.  A banner with Nanotrasen's logo is attached, that can't be removed."
	icon_state = "bannerpack"

/obj/item/storage/backpack/bannerpack/Initialize()
	. = ..()
	var/datum/component/storage/STR = GetComponent(/datum/component/storage)
	STR.max_combined_w_class = 27 //6 more then normal, for the tradeoff of declaring yourself an antag at all times.

/obj/item/storage/backpack/bannerpack/red
	name = "red banner backpack"
	desc = "It's a backpack with lots of extra room.  A red banner is attached, that can't be removed."
	icon_state = "bannerpack-red"

/obj/item/storage/backpack/bannerpack/blue
	name = "blue banner backpack"
	desc = "It's a backpack with lots of extra room.  A blue banner is attached, that can't be removed."
	icon_state = "bannerpack-blue"

//this is all part of one item set
/obj/item/clothing/suit/armor/plate/crusader
	name = "Crusader's Armour"
	desc = "Armour that's comprised of metal and cloth."
	icon_state = "crusader"
	w_class = WEIGHT_CLASS_BULKY
	slowdown = 2.0 //gotta pretend we're balanced.
	body_parts_covered = CHEST|GROIN|LEGS|FEET|ARMS|HANDS
	armor = list("melee" = 50, "bullet" = 50, "laser" = 50, "energy" = 50, "bomb" = 60, "bio" = 0, "rad" = 0, "fire" = 60, "acid" = 60)

/obj/item/clothing/suit/armor/plate/crusader/red
	icon_state = "crusader-red"

/obj/item/clothing/suit/armor/plate/crusader/blue
	icon_state = "crusader-blue"

/obj/item/clothing/head/helmet/plate/crusader
	name = "Crusader's Hood"
	desc = "A brownish hood."
	icon_state = "crusader"
	w_class = WEIGHT_CLASS_NORMAL
	flags_inv = HIDEHAIR|HIDEEARS|HIDEFACE
	armor = list("melee" = 50, "bullet" = 50, "laser" = 50, "energy" = 50, "bomb" = 60, "bio" = 0, "rad" = 0, "fire" = 60, "acid" = 60)

/obj/item/clothing/head/helmet/plate/crusader/blue
	icon_state = "crusader-blue"

/obj/item/clothing/head/helmet/plate/crusader/red
	icon_state = "crusader-red"

//Prophet helmet
/obj/item/clothing/head/helmet/plate/crusader/prophet
	name = "Prophet's Hat"
	desc = "A religious-looking hat."
	mob_overlay_icon = 'icons/mob/large-worn-icons/64x64/head.dmi'
	flags_1 = 0
	armor = list("melee" = 60, "bullet" = 60, "laser" = 60, "energy" = 60, "bomb" = 70, "bio" = 50, "rad" = 50, "fire" = 60, "acid" = 60) //religion protects you from disease and radiation, honk.
	worn_x_dimension = 64
	worn_y_dimension = 64

/obj/item/clothing/head/helmet/plate/crusader/prophet/red
	icon_state = "prophet-red"

/obj/item/clothing/head/helmet/plate/crusader/prophet/blue
	icon_state = "prophet-blue"

//Structure conversion staff
/obj/item/godstaff
	name = "godstaff"
	desc = "It's a stick..?"
	icon_state = "godstaff-red"
	lefthand_file = 'icons/mob/inhands/weapons/staves_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/weapons/staves_righthand.dmi'
	var/conversion_color = "#ffffff"
	var/staffcooldown = 0
	var/staffwait = 30


/obj/item/godstaff/afterattack(atom/target, mob/user, proximity_flag, click_parameters)
	. = ..()
	if(staffcooldown + staffwait > world.time)
		return
	user.visible_message("<span class='notice'>[user] chants deeply and waves [user.p_their()] staff!</span>")
	if(do_after(user, 20,1,src))
		target.add_atom_colour(conversion_color, WASHABLE_COLOUR_PRIORITY) //wololo
	staffcooldown = world.time

/obj/item/godstaff/red
	icon_state = "godstaff-red"
	conversion_color = "#ff0000"

/obj/item/godstaff/blue
	icon_state = "godstaff-blue"
	conversion_color = "#0000ff"

/obj/item/clothing/gloves/plate
	name = "Plate Gauntlets"
	icon_state = "crusader"
	desc = "They're like gloves, but made of metal."
	siemens_coefficient = 0
	cold_protection = HANDS
	min_cold_protection_temperature = GLOVES_MIN_TEMP_PROTECT
	heat_protection = HANDS
	max_heat_protection_temperature = GLOVES_MAX_TEMP_PROTECT

/obj/item/clothing/gloves/plate/red
	icon_state = "crusader-red"

/obj/item/clothing/gloves/plate/blue
	icon_state = "crusader-blue"

/obj/item/clothing/shoes/plate
	name = "Plate Boots"
	desc = "Metal boots, they look heavy."
	icon_state = "crusader"
	w_class = WEIGHT_CLASS_NORMAL
	armor = list("melee" = 50, "bullet" = 50, "laser" = 50, "energy" = 50, "bomb" = 60, "bio" = 0, "rad" = 0, "fire" = 60, "acid" = 60) //does this even do anything on boots?
	clothing_flags = NOSLIP
	cold_protection = FEET
	min_cold_protection_temperature = SHOES_MIN_TEMP_PROTECT
	heat_protection = FEET
	max_heat_protection_temperature = SHOES_MAX_TEMP_PROTECT


/obj/item/clothing/shoes/plate/red
	icon_state = "crusader-red"

/obj/item/clothing/shoes/plate/blue
	icon_state = "crusader-blue"


/obj/item/storage/box/itemset/crusader
	name = "Crusader's Armour Set" //i can't into ck2 references
	desc = "This armour is said to be based on the armor of kings on another world thousands of years ago, who tended to assassinate, conspire, and plot against everyone who tried to do the same to them.  Some things never change."


/obj/item/storage/box/itemset/crusader/blue/PopulateContents()
	new /obj/item/clothing/suit/armor/plate/crusader/blue(src)
	new /obj/item/clothing/head/helmet/plate/crusader/blue(src)
	new /obj/item/clothing/gloves/plate/blue(src)
	new /obj/item/clothing/shoes/plate/blue(src)


/obj/item/storage/box/itemset/crusader/red/PopulateContents()
	new /obj/item/clothing/suit/armor/plate/crusader/red(src)
	new /obj/item/clothing/head/helmet/plate/crusader/red(src)
	new /obj/item/clothing/gloves/plate/red(src)
	new /obj/item/clothing/shoes/plate/red(src)


/obj/item/claymore/weak
	desc = "This one is rusted."
	force = 30
	armour_penetration = 15

/obj/item/claymore/weak/ceremonial
	desc = "A rusted claymore, once at the heart of a powerful scottish clan struck down and oppressed by tyrants, it has been passed down the ages as a symbol of defiance."
	force = 15
	block_chance = 30
	armour_penetration = 5
