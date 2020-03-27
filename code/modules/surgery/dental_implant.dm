/datum/surgery/dental_implant
	name = "Dental implant"
	steps = list(/datum/surgery_step/drill, /datum/surgery_step/insert_pill)
	possible_locs = list(BODY_ZONE_PRECISE_MOUTH)

/datum/surgery_step/insert_pill
	name = "insert pill"
	implements = list(/obj/item/reagent_containers/pill = 100)
	time = 16

/datum/surgery_step/insert_pill/preop(mob/user, mob/living/carbon/target, target_zone, obj/item/tool, datum/surgery/surgery)
	display_results(user, target, "<span class='notice'>You begin to wedge [tool] in [target]'s [parse_zone(target_zone)]...</span>",
			"<span class='notice'>[user] begins to wedge \the [tool] in [target]'s [parse_zone(target_zone)].</span>",
			"<span class='notice'>[user] begins to wedge something in [target]'s [parse_zone(target_zone)].</span>")

/datum/surgery_step/insert_pill/success(mob/user, mob/living/carbon/target, target_zone, obj/item/reagent_containers/pill/tool, datum/surgery/surgery, default_display_results = FALSE)
	if(!istype(tool))
		return 0

	user.transferItemToLoc(tool, target, TRUE)

	var/datum/action/item_action/hands_free/activate_pill/P = new(tool)
	P.button.name = "Activate [tool.name]"
	P.target = tool
	P.Grant(target)	//The pill never actually goes in an inventory slot, so the owner doesn't inherit actions from it

	display_results(user, target, "<span class='notice'>You wedge [tool] into [target]'s [parse_zone(target_zone)].</span>",
			"<span class='notice'>[user] wedges \the [tool] into [target]'s [parse_zone(target_zone)]!</span>",
			"<span class='notice'>[user] wedges something into [target]'s [parse_zone(target_zone)]!</span>")
	return ..()

/datum/action/item_action/hands_free/activate_pill
	name = "Activate Pill"

/datum/action/item_action/hands_free/activate_pill/Trigger()
	if(!..())
		return FALSE
	to_chat(owner, "<span class='notice'>You grit your teeth and burst the implanted [target.name]!</span>")
	log_combat(owner, null, "swallowed an implanted pill", target)
	if(target.reagents.total_volume)
		target.reagents.reaction(owner, INGEST)
		target.reagents.trans_to(owner, target.reagents.total_volume, transfered_by = owner)
	qdel(target)
	return TRUE
