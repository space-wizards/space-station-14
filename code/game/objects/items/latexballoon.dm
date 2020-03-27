/obj/item/latexballon
	name = "latex glove"
	desc = "Sterile and airtight."
	icon_state = "latexballon"
	item_state = "lgloves"
	force = 0
	throwforce = 0
	w_class = WEIGHT_CLASS_TINY
	throw_speed = 1
	throw_range = 7
	var/state
	var/datum/gas_mixture/air_contents = null

/obj/item/latexballon/proc/blow(obj/item/tank/tank, mob/user)
	if (icon_state == "latexballon_bursted")
		return
	icon_state = "latexballon_blow"
	item_state = "latexballon"
	user.update_inv_hands()
	to_chat(user, "<span class='notice'>You blow up [src] with [tank].</span>")
	air_contents = tank.remove_air_volume(3)

/obj/item/latexballon/proc/burst()
	if (!air_contents || icon_state != "latexballon_blow")
		return
	playsound(src, 'sound/weapons/gun/pistol/shot.ogg', 100, TRUE)
	icon_state = "latexballon_bursted"
	item_state = "lgloves"
	if(isliving(loc))
		var/mob/living/user = src.loc
		user.update_inv_hands()
	loc.assume_air(air_contents)

/obj/item/latexballon/ex_act(severity, target)
	burst()
	switch(severity)
		if (1)
			qdel(src)
		if (2)
			if (prob(50))
				qdel(src)

/obj/item/latexballon/bullet_act(obj/projectile/P)
	if(!P.nodamage)
		burst()
	return ..()

/obj/item/latexballon/temperature_expose(datum/gas_mixture/air, temperature, volume)
	if(temperature > T0C+100)
		burst()

/obj/item/latexballon/attackby(obj/item/W, mob/user, params)
	if(istype(W, /obj/item/tank))
		var/obj/item/tank/T = W
		blow(T, user)
		return
	if (W.get_sharpness() || W.get_temperature() || is_pointed(W))
		burst()
