/obj/item/assembly/shock_kit
	name = "electrohelmet assembly"
	desc = "This appears to be made from both an electropack and a helmet."
	icon = 'icons/obj/assemblies.dmi'
	icon_state = "shock_kit"
	var/obj/item/clothing/head/helmet/part1 = null
	var/obj/item/electropack/part2 = null
	w_class = WEIGHT_CLASS_HUGE
	flags_1 = CONDUCT_1

/obj/item/assembly/shock_kit/Destroy()
	qdel(part1)
	qdel(part2)
	return ..()

/obj/item/assembly/shock_kit/wrench_act(mob/living/user, obj/item/I)
	..()
	to_chat(user, "<span class='notice'>You disassemble [src].</span>")
	if(part1)
		part1.forceMove(drop_location())
		part1.master = null
		part1 = null
	if(part2)
		part2.forceMove(drop_location())
		part2.master = null
		part2 = null
	qdel(src)
	return TRUE

/obj/item/assembly/shock_kit/attack_self(mob/user)
	part1.attack_self(user)
	part2.attack_self(user)
	add_fingerprint(user)
	return

/obj/item/assembly/shock_kit/receive_signal()
	if(istype(loc, /obj/structure/chair/e_chair))
		var/obj/structure/chair/e_chair/C = loc
		C.shock()
	return
