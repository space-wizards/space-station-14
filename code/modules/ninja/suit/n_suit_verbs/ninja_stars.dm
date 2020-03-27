

//Creates a throwing star
/obj/item/clothing/suit/space/space_ninja/proc/ninjastar()
	if(!ninjacost(10))
		var/mob/living/carbon/human/H = affecting
		var/obj/item/throwing_star/ninja/N = new(H)
		if(H.put_in_hands(N))
			to_chat(H, "<span class='notice'>A throwing star has been created in your hand!</span>")
		else
			qdel(N)
		H.throw_mode_on() //So they can quickly throw it.


/obj/item/throwing_star/ninja
	name = "ninja throwing star"
	throwforce = 30
	embedding = list("embedded_pain_multiplier" = 6, "embed_chance" = 100, "embedded_fall_chance" = 0)
