//Used by engineering cyborgs in place of generic circuits.
/obj/item/electroadaptive_pseudocircuit
	name = "electroadaptive pseudocircuit"
	desc = "An all-in-one circuit imprinter, designer, synthesizer, outfitter, creator, and chef. It can be used in place of any generic circuit board during construction."
	icon = 'icons/obj/module.dmi'
	icon_state = "boris"
	w_class = WEIGHT_CLASS_TINY
	custom_materials = list(/datum/material/iron = 50, /datum/material/glass = 300)
	var/recharging = FALSE
	var/circuits = 5 //How many circuits the pseudocircuit has left
	var/static/recycleable_circuits = typecacheof(list(/obj/item/electronics/firelock, /obj/item/electronics/airalarm, /obj/item/electronics/firealarm, \
	/obj/item/electronics/apc))//A typecache of circuits consumable for material

/obj/item/electroadaptive_pseudocircuit/Initialize()
	. = ..()
	maptext = "[circuits]"

/obj/item/electroadaptive_pseudocircuit/examine(mob/user)
	. = ..()
	if(iscyborg(user))
		. += "<span class='notice'>It has material for <b>[circuits]</b> circuit[circuits == 1 ? "" : "s"]. Use the pseudocircuit on existing circuits to gain material.</span>\n"+\
		"<span class='notice'>Serves as a substitute for <b>fire/air alarm</b>, <b>firelock</b>, and <b>APC</b> electronics.</span>\n"+\
		"<span class='notice'>It can also be used on an APC with no power cell to <b>fabricate a low-capacity cell</b> at a high power cost.</span>"

/obj/item/electroadaptive_pseudocircuit/proc/adapt_circuit(mob/living/silicon/robot/R, circuit_cost = 0)
	if(QDELETED(R) || !istype(R))
		return
	if(!R.cell)
		to_chat(R, "<span class='warning'>You need a power cell installed for that.</span>")
		return
	if(!R.cell.use(circuit_cost))
		to_chat(R, "<span class='warning'>You don't have the energy for that (you need [DisplayEnergy(circuit_cost)].)</span>")
		return
	if(recharging)
		to_chat(R, "<span class='warning'>[src] needs some time to recharge first.</span>")
		return
	if(!circuits)
		to_chat(R, "<span class='warning'>You need more material. Use [src] on existing simple circuits to break them down.</span>")
		return
	playsound(R, 'sound/items/rped.ogg', 50, TRUE)
	recharging = TRUE
	circuits--
	maptext = "[circuits]"
	icon_state = "[initial(icon_state)]_recharging"
	var/recharge_time = min(600, circuit_cost * 5)  //40W of cost for one fabrication = 20 seconds of recharge time; this is to prevent spamming
	addtimer(CALLBACK(src, .proc/recharge), recharge_time)
	return TRUE //The actual circuit magic itself is done on a per-object basis

/obj/item/electroadaptive_pseudocircuit/afterattack(atom/target, mob/living/user, proximity)
	. = ..()
	if(!proximity)
		return
	if(!is_type_in_typecache(target, recycleable_circuits))
		return
	circuits++
	maptext = "[circuits]"
	user.visible_message("<span class='notice'>User breaks down [target] with [src].</span>", \
	"<span class='notice'>You recycle [target] into [src]. It now has material for <b>[circuits]</b> circuits.</span>")
	playsound(user, 'sound/items/deconstruct.ogg', 50, TRUE)
	qdel(target)

/obj/item/electroadaptive_pseudocircuit/proc/recharge()
	playsound(src, 'sound/machines/chime.ogg', 25, TRUE)
	recharging = FALSE
	icon_state = initial(icon_state)
