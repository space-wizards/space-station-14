/obj/item/organ/heart/gland/plasma
	true_name = "effluvium sanguine-synonym emitter"
	cooldown_low = 1200
	cooldown_high = 1800
	icon_state = "slime"
	uses = -1
	mind_control_uses = 1
	mind_control_duration = 800

/obj/item/organ/heart/gland/plasma/activate()
	to_chat(owner, "<span class='warning'>You feel bloated.</span>")
	addtimer(CALLBACK(GLOBAL_PROC, .proc/to_chat, owner, "<span class='userdanger'>A massive stomachache overcomes you.</span>"), 150)
	addtimer(CALLBACK(src, .proc/vomit_plasma), 200)

/obj/item/organ/heart/gland/plasma/proc/vomit_plasma()
	if(!owner)
		return
	owner.visible_message("<span class='danger'>[owner] vomits a cloud of plasma!</span>")
	var/turf/open/T = get_turf(owner)
	if(istype(T))
		T.atmos_spawn_air("plasma=50;TEMP=[T20C]")
	owner.vomit()
