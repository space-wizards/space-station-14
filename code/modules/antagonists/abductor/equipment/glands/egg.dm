/obj/item/organ/heart/gland/egg
	true_name = "roe/enzymatic synthesizer"
	cooldown_low = 300
	cooldown_high = 400
	uses = -1
	icon_state = "egg"
	lefthand_file = 'icons/mob/inhands/misc/food_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/misc/food_righthand.dmi'
	mind_control_uses = 2
	mind_control_duration = 1800

/obj/item/organ/heart/gland/egg/activate()
	owner.visible_message("<span class='alertalien'>[owner] [pick(EGG_LAYING_MESSAGES)]</span>")
	var/turf/T = owner.drop_location()
	new /obj/item/reagent_containers/food/snacks/egg/gland(T)
