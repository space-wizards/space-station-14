#define C2NAMEREAGENT	"[initial(reagent.name)] (Has Side-Effects)"
/*
Contains:
Borg Hypospray
Borg Shaker
Nothing to do with hydroponics in here. Sorry to dissapoint you.
*/

/*
Borg Hypospray
*/
/obj/item/reagent_containers/borghypo
	name = "cyborg hypospray"
	desc = "An advanced chemical synthesizer and injection system, designed for heavy-duty medical equipment."
	icon = 'icons/obj/syringe.dmi'
	item_state = "hypo"
	lefthand_file = 'icons/mob/inhands/equipment/medical_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/equipment/medical_righthand.dmi'
	icon_state = "borghypo"
	amount_per_transfer_from_this = 5
	volume = 30
	possible_transfer_amounts = list()
	var/mode = 1
	var/charge_cost = 50
	var/charge_tick = 0
	var/recharge_time = 5 //Time it takes for shots to recharge (in seconds)
	var/bypass_protection = 0 //If the hypospray can go through armor or thick material

	var/list/datum/reagents/reagent_list = list()
	var/list/reagent_ids = list(/datum/reagent/medicine/C2/convermol, /datum/reagent/medicine/C2/libital, /datum/reagent/medicine/C2/multiver, /datum/reagent/medicine/C2/aiuri, /datum/reagent/medicine/epinephrine, /datum/reagent/medicine/spaceacillin, /datum/reagent/medicine/salglu_solution)
	var/accepts_reagent_upgrades = TRUE //If upgrades can increase number of reagents dispensed.
	var/list/modes = list() //Basically the inverse of reagent_ids. Instead of having numbers as "keys" and strings as values it has strings as keys and numbers as values.
								//Used as list for input() in shakers.
	var/list/reagent_names = list()


/obj/item/reagent_containers/borghypo/Initialize()
	. = ..()

	for(var/R in reagent_ids)
		add_reagent(R)

	START_PROCESSING(SSobj, src)


/obj/item/reagent_containers/borghypo/Destroy()
	STOP_PROCESSING(SSobj, src)
	return ..()


/obj/item/reagent_containers/borghypo/process() //Every [recharge_time] seconds, recharge some reagents for the cyborg
	charge_tick++
	if(charge_tick >= recharge_time)
		regenerate_reagents()
		charge_tick = 0

	//update_icon()
	return 1

// Use this to add more chemicals for the borghypo to produce.
/obj/item/reagent_containers/borghypo/proc/add_reagent(datum/reagent/reagent)
	reagent_ids |= reagent
	var/datum/reagents/RG = new(30)
	RG.my_atom = src
	reagent_list += RG

	var/datum/reagents/R = reagent_list[reagent_list.len]
	R.add_reagent(reagent, 30)

	modes[reagent] = modes.len + 1

	if(initial(reagent.harmful))
		reagent_names[C2NAMEREAGENT] = reagent
	else
		reagent_names[initial(reagent.name)] = reagent

/obj/item/reagent_containers/borghypo/proc/del_reagent(datum/reagent/reagent)
	reagent_ids -= reagent
	if(istype(reagent, /datum/reagent/medicine/C2))
		reagent_names -= C2NAMEREAGENT
	else
		reagent_names -= initial(reagent.name)
	var/datum/reagents/RG
	var/datum/reagents/TRG
	for(var/i in 1 to reagent_ids.len)
		TRG = reagent_list[i]
		if (TRG.has_reagent(reagent))
			RG = TRG
			break
	if (RG)
		reagent_list -= RG
		RG.del_reagent(reagent)

		modes[reagent] = modes.len - 1

/obj/item/reagent_containers/borghypo/proc/regenerate_reagents()
	if(iscyborg(src.loc))
		var/mob/living/silicon/robot/R = src.loc
		if(R && R.cell)
			for(var/i in 1 to reagent_ids.len)
				var/datum/reagents/RG = reagent_list[i]
				if(RG.total_volume < RG.maximum_volume) 	//Don't recharge reagents and drain power if the storage is full.
					R.cell.use(charge_cost) 					//Take power from borg...
					RG.add_reagent(reagent_ids[i], 5)		//And fill hypo with reagent.

/obj/item/reagent_containers/borghypo/attack(mob/living/carbon/M, mob/user)
	var/datum/reagents/R = reagent_list[mode]
	if(!R.total_volume)
		to_chat(user, "<span class='warning'>The injector is empty!</span>")
		return
	if(!istype(M))
		return
	if(R.total_volume && M.can_inject(user, 1, user.zone_selected,bypass_protection))
		to_chat(M, "<span class='warning'>You feel a tiny prick!</span>")
		to_chat(user, "<span class='notice'>You inject [M] with the injector.</span>")
		if(M.reagents)
			var/trans = R.trans_to(M, amount_per_transfer_from_this, transfered_by = user, method = INJECT)
			to_chat(user, "<span class='notice'>[trans] unit\s injected. [R.total_volume] unit\s remaining.</span>")

	var/list/injected = list()
	for(var/datum/reagent/RG in R.reagent_list)
		injected += RG.name
	log_combat(user, M, "injected", src, "(CHEMICALS: [english_list(injected)])")

/obj/item/reagent_containers/borghypo/attack_self(mob/user)
	var/chosen_reagent = modes[reagent_names[input(user, "What reagent do you want to dispense?") as null|anything in sortList(reagent_names)]]
	if(!chosen_reagent)
		return
	mode = chosen_reagent
	playsound(loc, 'sound/effects/pop.ogg', 50, FALSE)
	var/datum/reagent/R = GLOB.chemical_reagents_list[reagent_ids[mode]]
	to_chat(user, "<span class='notice'>[src] is now dispensing '[R.name]'.</span>")
	return

/obj/item/reagent_containers/borghypo/examine(mob/user)
	. = ..()
	. += DescribeContents()	//Because using the standardized reagents datum was just too cool for whatever fuckwit wrote this
	var/datum/reagent/loaded = modes[mode]
	. += "Currently loaded: [initial(loaded.name)]. [initial(loaded.description)]"
	. += "<span class='notice'><i>Alt+Click</i> to change transfer amount. Currently set to [amount_per_transfer_from_this == 5 ? "dose normally (5u)" : "microdose (2u)"].</span>"

/obj/item/reagent_containers/borghypo/proc/DescribeContents()
	. = list()
	var/empty = TRUE

	for(var/datum/reagents/RS in reagent_list)
		var/datum/reagent/R = locate() in RS.reagent_list
		if(R)
			. += "<span class='notice'>It currently has [R.volume] unit\s of [R.name] stored.</span>"
			empty = FALSE

	if(empty)
		. += "<span class='warning'>It is currently empty! Allow some time for the internal synthesizer to produce more.</span>"

/obj/item/reagent_containers/borghypo/AltClick(mob/living/user)
	. = ..()
	if(user.stat == DEAD || user != loc)
		return //IF YOU CAN HEAR ME SET MY TRANSFER AMOUNT TO 1
	if(amount_per_transfer_from_this == 5)
		amount_per_transfer_from_this = 2
	else
		amount_per_transfer_from_this = 5
	to_chat(user,"<span class='notice'>[src] is now set to [amount_per_transfer_from_this == 5 ? "dose normally" : "microdose"].</span>")

/obj/item/reagent_containers/borghypo/hacked
	icon_state = "borghypo_s"
	reagent_ids = list (/datum/reagent/toxin/acid/fluacid, /datum/reagent/toxin/mutetoxin, /datum/reagent/toxin/cyanide, /datum/reagent/toxin/sodium_thiopental, /datum/reagent/toxin/heparin, /datum/reagent/toxin/lexorin)
	accepts_reagent_upgrades = FALSE

/obj/item/reagent_containers/borghypo/clown
	name = "laughter injector"
	desc = "Keeps the crew happy and productive!"
	reagent_ids = list(/datum/reagent/consumable/laughter)
	accepts_reagent_upgrades = FALSE

/obj/item/reagent_containers/borghypo/clown/hacked
	name = "laughter injector"
	desc = "Keeps the crew so happy they don't work!"
	reagent_ids = list(/datum/reagent/consumable/superlaughter)
	accepts_reagent_upgrades = FALSE

/obj/item/reagent_containers/borghypo/syndicate
	name = "syndicate cyborg hypospray"
	desc = "An experimental piece of Syndicate technology used to produce powerful restorative nanites used to very quickly restore injuries of all types. Also metabolizes potassium iodide, for radiation poisoning, and morphine, for offense."
	icon_state = "borghypo_s"
	charge_cost = 20
	recharge_time = 2
	reagent_ids = list(/datum/reagent/medicine/syndicate_nanites, /datum/reagent/medicine/potass_iodide, /datum/reagent/medicine/morphine)
	bypass_protection = 1
	accepts_reagent_upgrades = FALSE

/*
Borg Shaker
*/
/obj/item/reagent_containers/borghypo/borgshaker
	name = "cyborg shaker"
	desc = "An advanced drink synthesizer and mixer."
	icon = 'icons/obj/drinks.dmi'
	icon_state = "shaker"
	possible_transfer_amounts = list(5,10,20)
	charge_cost = 20 //Lots of reagents all regenerating at once, so the charge cost is lower. They also regenerate faster.
	recharge_time = 3
	accepts_reagent_upgrades = FALSE

	reagent_ids = list(/datum/reagent/consumable/applejuice, /datum/reagent/consumable/banana, /datum/reagent/consumable/coffee, 
	/datum/reagent/consumable/cream, /datum/reagent/consumable/dr_gibb, /datum/reagent/consumable/grenadine, 
	/datum/reagent/consumable/ice, /datum/reagent/consumable/lemonjuice, /datum/reagent/consumable/lemon_lime, 
	/datum/reagent/consumable/limejuice, /datum/reagent/consumable/menthol, /datum/reagent/consumable/milk, 
	/datum/reagent/consumable/nothing, /datum/reagent/consumable/orangejuice, /datum/reagent/consumable/peachjuice, 
	/datum/reagent/consumable/sodawater, /datum/reagent/consumable/space_cola, /datum/reagent/consumable/spacemountainwind, 
	/datum/reagent/consumable/pwr_game, /datum/reagent/consumable/shamblers, /datum/reagent/consumable/soymilk, 
	/datum/reagent/consumable/space_up, /datum/reagent/consumable/sugar, /datum/reagent/consumable/tea, 
	/datum/reagent/consumable/tomatojuice, /datum/reagent/consumable/tonic, /datum/reagent/water, 
	/datum/reagent/consumable/ethanol/ale, /datum/reagent/consumable/ethanol/applejack, /datum/reagent/consumable/ethanol/beer, 
	/datum/reagent/consumable/ethanol/champagne, /datum/reagent/consumable/ethanol/cognac, /datum/reagent/consumable/ethanol/creme_de_menthe, 
	/datum/reagent/consumable/ethanol/creme_de_cacao, /datum/reagent/consumable/ethanol/gin, /datum/reagent/consumable/ethanol/kahlua, 
	/datum/reagent/consumable/ethanol/rum, /datum/reagent/consumable/ethanol/sake, /datum/reagent/consumable/ethanol/tequila, 
	/datum/reagent/consumable/ethanol/triple_sec, /datum/reagent/consumable/ethanol/vermouth, /datum/reagent/consumable/ethanol/vodka, 
	/datum/reagent/consumable/ethanol/whiskey, /datum/reagent/consumable/ethanol/wine)

/obj/item/reagent_containers/borghypo/borgshaker/attack(mob/M, mob/user)
	return //Can't inject stuff with a shaker, can we? //not with that attitude

/obj/item/reagent_containers/borghypo/borgshaker/regenerate_reagents()
	if(iscyborg(src.loc))
		var/mob/living/silicon/robot/R = src.loc
		if(R && R.cell)
			for(var/i in modes) //Lots of reagents in this one, so it's best to regenrate them all at once to keep it from being tedious.
				var/valueofi = modes[i]
				var/datum/reagents/RG = reagent_list[valueofi]
				if(RG.total_volume < RG.maximum_volume)
					R.cell.use(charge_cost)
					RG.add_reagent(reagent_ids[valueofi], 5)

/obj/item/reagent_containers/borghypo/borgshaker/afterattack(obj/target, mob/user, proximity)
	. = ..()
	if(!proximity)
		return

	else if(target.is_refillable())
		var/datum/reagents/R = reagent_list[mode]
		if(!R.total_volume)
			to_chat(user, "<span class='warning'>[src] is currently out of this ingredient! Please allow some time for the synthesizer to produce more.</span>")
			return

		if(target.reagents.total_volume >= target.reagents.maximum_volume)
			to_chat(user, "<span class='notice'>[target] is full.</span>")
			return

		var/trans = R.trans_to(target, amount_per_transfer_from_this, transfered_by = user)
		to_chat(user, "<span class='notice'>You transfer [trans] unit\s of the solution to [target].</span>")

/obj/item/reagent_containers/borghypo/borgshaker/DescribeContents()
	var/datum/reagents/RS = reagent_list[mode]
	var/datum/reagent/R = locate() in RS.reagent_list
	if(R)
		return "<span class='notice'>It currently has [R.volume] unit\s of [R.name] stored.</span>"
	else
		return "<span class='warning'>It is currently empty! Please allow some time for the synthesizer to produce more.</span>"

/obj/item/reagent_containers/borghypo/borgshaker/hacked
	name = "cyborg shaker"
	desc = "Will mix drinks that knock them dead."
	icon = 'icons/obj/drinks.dmi'
	icon_state = "threemileislandglass"
	possible_transfer_amounts = list(5,10,20)
	charge_cost = 20 //Lots of reagents all regenerating at once, so the charge cost is lower. They also regenerate faster.
	recharge_time = 3
	accepts_reagent_upgrades = FALSE

	reagent_ids = list(/datum/reagent/toxin/fakebeer, /datum/reagent/consumable/ethanol/fernet)

/obj/item/reagent_containers/borghypo/peace
	name = "Peace Hypospray"

	reagent_ids = list(/datum/reagent/peaceborg/confuse,/datum/reagent/peaceborg/tire,/datum/reagent/pax/peaceborg)
	accepts_reagent_upgrades = FALSE

/obj/item/reagent_containers/borghypo/peace/hacked
	desc = "Everything's peaceful in death!"
	icon_state = "borghypo_s"
	reagent_ids = list(/datum/reagent/peaceborg/confuse,/datum/reagent/peaceborg/tire,/datum/reagent/pax/peaceborg,/datum/reagent/toxin/staminatoxin,/datum/reagent/toxin/sulfonal,/datum/reagent/toxin/sodium_thiopental,/datum/reagent/toxin/cyanide,/datum/reagent/toxin/fentanyl)
	accepts_reagent_upgrades = FALSE

/obj/item/reagent_containers/borghypo/epi
	name = "epinephrine injector"
	desc = "An advanced chemical synthesizer and injection system, designed to stabilize patients."
	reagent_ids = list(/datum/reagent/medicine/epinephrine)
	accepts_reagent_upgrades = FALSE

#undef C2NAMEREAGENT
