/obj/item/organ/heart/gland/access
	true_name = "anagraphic electro-scrambler"
	cooldown_low = 600
	cooldown_high = 1200
	uses = 1
	icon_state = "mindshock"
	mind_control_uses = 3
	mind_control_duration = 900

/obj/item/organ/heart/gland/access/activate()
	to_chat(owner, "<span class='notice'>You feel like a VIP for some reason.</span>")
	RegisterSignal(owner, COMSIG_MOB_ALLOWED, .proc/free_access)

/obj/item/organ/heart/gland/access/proc/free_access(datum/source, obj/O)
	return TRUE

/obj/item/organ/heart/gland/access/Remove(mob/living/carbon/M, special = 0)
	UnregisterSignal(owner, COMSIG_MOB_ALLOWED)
	..()
