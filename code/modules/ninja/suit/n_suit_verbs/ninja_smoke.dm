

//Smoke bomb
/obj/item/clothing/suit/space/space_ninja/proc/ninjasmoke()

	if(!ninjacost(0,N_SMOKE_BOMB))
		var/mob/living/carbon/human/H = affecting
		var/datum/effect_system/smoke_spread/bad/smoke = new
		smoke.set_up(4, H.loc)
		smoke.start()
		playsound(H.loc, 'sound/effects/bamf.ogg', 50, 2)
		s_bombs--
		to_chat(H, "<span class='notice'>There are <B>[s_bombs]</B> smoke bombs remaining.</span>")
		s_coold = 1
