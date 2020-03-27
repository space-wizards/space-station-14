/obj/effect/proc_holder/spell/targeted/explosion
	name = "Explosion"
	desc = "This spell explodes an area."

	var/ex_severe = 1
	var/ex_heavy = 2
	var/ex_light = 3
	var/ex_flash = 4

/obj/effect/proc_holder/spell/targeted/explosion/cast(list/targets,mob/user = usr)
	for(var/mob/living/target in targets)
		if(target.anti_magic_check())
			continue
		explosion(target.loc,ex_severe,ex_heavy,ex_light,ex_flash)

	return
