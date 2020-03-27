/obj/item/organ/heart/gland/quantum
	true_name = "quantic de-observation matrix"
	cooldown_low = 150
	cooldown_high = 150
	uses = -1
	icon_state = "emp"
	mind_control_uses = 2
	mind_control_duration = 1200
	var/mob/living/carbon/entangled_mob

/obj/item/organ/heart/gland/quantum/activate()
	if(entangled_mob)
		return
	for(var/mob/M in oview(owner, 7))
		if(!iscarbon(M))
			continue
		entangled_mob = M
		addtimer(CALLBACK(src, .proc/quantum_swap), rand(600, 2400))
		return

/obj/item/organ/heart/gland/quantum/proc/quantum_swap()
	if(QDELETED(entangled_mob))
		entangled_mob = null
		return
	var/turf/T = get_turf(owner)
	do_teleport(owner, get_turf(entangled_mob),null,TRUE,channel = TELEPORT_CHANNEL_QUANTUM)
	do_teleport(entangled_mob, T,null,TRUE,channel = TELEPORT_CHANNEL_QUANTUM)
	to_chat(owner, "<span class='warning'>You suddenly find yourself somewhere else!</span>")
	to_chat(entangled_mob, "<span class='warning'>You suddenly find yourself somewhere else!</span>")
	if(!active_mind_control) //Do not reset entangled mob while mind control is active
		entangled_mob = null

/obj/item/organ/heart/gland/quantum/mind_control(command, mob/living/user)
	if(..())
		if(entangled_mob && ishuman(entangled_mob) && (entangled_mob.stat < DEAD))
			to_chat(entangled_mob, "<span class='userdanger'>You suddenly feel an irresistible compulsion to follow an order...</span>")
			to_chat(entangled_mob, "<span class='mind_control'>[command]</span>")
			var/obj/screen/alert/mind_control/mind_alert = entangled_mob.throw_alert("mind_control", /obj/screen/alert/mind_control)
			mind_alert.command = command
			message_admins("[key_name(owner)] mirrored an abductor mind control message to [key_name(entangled_mob)]: [command]")
			update_gland_hud()

/obj/item/organ/heart/gland/quantum/clear_mind_control()
	if(active_mind_control)
		to_chat(entangled_mob, "<span class='userdanger'>You feel the compulsion fade, and you completely forget about your previous orders.</span>")
		entangled_mob.clear_alert("mind_control")
	..()
