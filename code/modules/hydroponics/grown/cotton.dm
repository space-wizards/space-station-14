/obj/item/seeds/cotton
	name = "pack of cotton seeds"
	desc = "A pack of seeds that'll grow into a cotton plant. Assistants make good free labor if neccesary."
	icon_state = "seed-cotton"
	species = "cotton"
	plantname = "Cotton"
	icon_harvest = "cotton-harvest"
	product = /obj/item/grown/cotton
	lifespan = 35
	endurance = 25
	maturation = 15
	production = 1
	yield = 2
	potency = 50
	growthstages = 3
	growing_icon = 'icons/obj/hydroponics/growing.dmi'
	icon_dead = "cotton-dead"
	mutatelist = list(/obj/item/seeds/cotton/durathread)

/obj/item/grown/cotton
	seed = /obj/item/seeds/cotton
	name = "cotton bundle"
	desc = "A fluffy bundle of cotton."
	icon_state = "cotton"
	force = 0
	throwforce = 0
	w_class = WEIGHT_CLASS_TINY
	throw_speed = 2
	throw_range = 3
	attack_verb = list("pomfed")
	var/cotton_type = /obj/item/stack/sheet/cotton
	var/cotton_name = "raw cotton"

/obj/item/grown/cotton/attack_self(mob/user)
	user.show_message("<span class='notice'>You pull some [cotton_name] out of the [name]!</span>", MSG_VISUAL)
	var/seed_modifier = 0
	if(seed)
		seed_modifier = round(seed.potency / 25)
	var/obj/item/stack/cotton = new cotton_type(user.loc, 1 + seed_modifier)
	var/old_cotton_amount = cotton.amount
	for(var/obj/item/stack/ST in user.loc)
		if(ST != cotton && istype(ST, cotton_type) && ST.amount < ST.max_amount)
			ST.attackby(cotton, user)
	if(cotton.amount > old_cotton_amount)
		to_chat(user, "<span class='notice'>You add the newly-formed [cotton_name] to the stack. It now contains [cotton.amount] [cotton_name].</span>")
	qdel(src)

//reinforced mutated variant
/obj/item/seeds/cotton/durathread
	name = "pack of durathread seeds"
	desc = "A pack of seeds that'll grow into an extremely durable thread that could easily rival plasteel if woven properly."
	icon_state = "seed-durathread"
	species = "durathread"
	plantname = "Durathread"
	icon_harvest = "durathread-harvest"
	product = /obj/item/grown/cotton/durathread
	lifespan = 80
	endurance = 50
	maturation = 15
	production = 1
	yield = 2
	potency = 50
	growthstages = 3
	growing_icon = 'icons/obj/hydroponics/growing.dmi'
	icon_dead = "cotton-dead"

/obj/item/grown/cotton/durathread
	seed = /obj/item/seeds/cotton/durathread
	name = "durathread bundle"
	desc = "A tough bundle of durathread, good luck unraveling this."
	icon_state = "durathread"
	force = 5
	throwforce = 5
	w_class = WEIGHT_CLASS_NORMAL
	throw_speed = 2
	throw_range = 3
	attack_verb = list("bashed", "battered", "bludgeoned", "whacked")
	cotton_type = /obj/item/stack/sheet/cotton/durathread
	cotton_name = "raw durathread"
