/datum/surgery/cavity_implant
	name = "Cavity implant"
	steps = list(/datum/surgery_step/incise, /datum/surgery_step/clamp_bleeders, /datum/surgery_step/retract_skin, /datum/surgery_step/incise, /datum/surgery_step/handle_cavity, /datum/surgery_step/close)
	target_mobtypes = list(/mob/living/carbon/human, /mob/living/carbon/monkey)
	possible_locs = list(BODY_ZONE_CHEST)


//handle cavity
/datum/surgery_step/handle_cavity
	name = "implant item"
	accept_hand = 1
	implements = list(/obj/item = 100)
	repeatable = TRUE
	time = 32
	var/obj/item/IC = null

/datum/surgery_step/handle_cavity/tool_check(mob/user, obj/item/tool)
	if(tool.tool_behaviour == TOOL_CAUTERY || istype(tool, /obj/item/gun/energy/laser))
		return FALSE
	return !tool.get_temperature()

/datum/surgery_step/handle_cavity/preop(mob/user, mob/living/carbon/human/target, target_zone, obj/item/tool, datum/surgery/surgery)
	var/obj/item/bodypart/chest/CH = target.get_bodypart(BODY_ZONE_CHEST)
	IC = CH.cavity_item
	if(tool)
		display_results(user, target, "<span class='notice'>You begin to insert [tool] into [target]'s [target_zone]...</span>",
			"<span class='notice'>[user] begins to insert [tool] into [target]'s [target_zone].</span>",
			"<span class='notice'>[user] begins to insert [tool.w_class > WEIGHT_CLASS_SMALL ? tool : "something"] into [target]'s [target_zone].</span>")
	else
		display_results(user, target, "<span class='notice'>You check for items in [target]'s [target_zone]...</span>",
			"<span class='notice'>[user] checks for items in [target]'s [target_zone].</span>",
			"<span class='notice'>[user] looks for something in [target]'s [target_zone].</span>")

/datum/surgery_step/handle_cavity/success(mob/user, mob/living/carbon/human/target, target_zone, obj/item/tool, datum/surgery/surgery = FALSE)
	var/obj/item/bodypart/chest/CH = target.get_bodypart(BODY_ZONE_CHEST)
	if(tool)
		if(IC || tool.w_class > WEIGHT_CLASS_NORMAL || HAS_TRAIT(tool, TRAIT_NODROP) || istype(tool, /obj/item/organ))
			to_chat(user, "<span class='warning'>You can't seem to fit [tool] in [target]'s [target_zone]!</span>")
			return 0
		else
			display_results(user, target, "<span class='notice'>You stuff [tool] into [target]'s [target_zone].</span>",
				"<span class='notice'>[user] stuffs [tool] into [target]'s [target_zone]!</span>",
				"<span class='notice'>[user] stuffs [tool.w_class > WEIGHT_CLASS_SMALL ? tool : "something"] into [target]'s [target_zone].</span>")
			user.transferItemToLoc(tool, target, TRUE)
			CH.cavity_item = tool
			return ..()
	else
		if(IC)
			display_results(user, target, "<span class='notice'>You pull [IC] out of [target]'s [target_zone].</span>",
				"<span class='notice'>[user] pulls [IC] out of [target]'s [target_zone]!</span>",
				"<span class='notice'>[user] pulls [IC.w_class > WEIGHT_CLASS_SMALL ? IC : "something"] out of [target]'s [target_zone].</span>")
			user.put_in_hands(IC)
			CH.cavity_item = null
			return ..()
		else
			to_chat(user, "<span class='warning'>You don't find anything in [target]'s [target_zone].</span>")
			return 0
