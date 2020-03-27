#define SHOWCASE_CONSTRUCTED 1
#define SHOWCASE_SCREWDRIVERED 2

/*Completely generic structures for use by mappers to create fake objects, i.e. display rooms*/
/obj/structure/showcase
	name = "showcase"
	icon = 'icons/obj/stationobjs.dmi'
	icon_state = "showcase_1"
	desc = "A stand with the empty body of a cyborg bolted to it."
	density = TRUE
	anchored = TRUE
	var/deconstruction_state = SHOWCASE_CONSTRUCTED

/obj/structure/showcase/fakeid
	name = "\improper CentCom identification console"
	desc = "You can use this to change ID's."
	icon = 'icons/obj/computer.dmi'
	icon_state = "computer"

/obj/structure/showcase/fakeid/Initialize()
	. = ..()
	add_overlay("id")
	add_overlay("id_key")

/obj/structure/showcase/fakesec
	name = "\improper CentCom security records"
	desc = "Used to view and edit personnel's security records."
	icon = 'icons/obj/computer.dmi'
	icon_state = "computer"

/obj/structure/showcase/fakesec/Initialize()
	. = ..()
	add_overlay("security")
	add_overlay("security_key")

/obj/structure/showcase/horrific_experiment
	name = "horrific experiment"
	desc = "Some sort of pod filled with blood and viscera. You swear you can see it moving..."
	icon = 'icons/obj/machines/cloning.dmi'
	icon_state = "pod_g"

/obj/structure/showcase/machinery/oldpod
	name = "damaged cryogenic pod"
	desc = "A damaged cryogenic pod long since lost to time, including its former occupant..."
	icon = 'icons/obj/machines/sleeper.dmi'
	icon_state = "sleeper-open"

/obj/structure/showcase/machinery/oldpod/used
	name = "opened cryogenic pod"
	desc = "A cryogenic pod that has recently discharged its occupant. The pod appears non-functional."

/obj/structure/showcase/cyborg/old
	name = "Cyborg Statue"
	desc = "An old, deactivated cyborg. Whilst once actively used to guard against intruders, it now simply intimidates them with its cold, steely gaze."
	icon = 'icons/mob/robots.dmi'
	icon_state = "robot_old"
	density = FALSE

/obj/structure/showcase/mecha/marauder
	name = "combat mech exhibit"
	desc = "A stand with an empty old Nanotrasen Corporation combat mech bolted to it. It is described as the premier unit used to defend corporate interests and employees."
	icon = 'icons/mecha/mecha.dmi'
	icon_state = "marauder"

/obj/structure/showcase/mecha/ripley
	name = "construction mech exhibit"
	desc = "A stand with a retired construction mech bolted to it. The clamps are rated at 9300PSI. It seems to be falling apart."
	icon = 'icons/mecha/mecha.dmi'
	icon_state = "firefighter"

/obj/structure/showcase/machinery/implanter
	name = "Nanotrasen automated mindshield implanter exhibit"
	desc = "A flimsy model of a standard Nanotrasen automated mindshield implant machine. With secure positioning harnesses and a robotic surgical injector, brain damage and other serious medical anomalies are now up to 60% less likely!"
	icon = 'icons/obj/machines/implantchair.dmi'
	icon_state = "implantchair"

/obj/structure/showcase/machinery/microwave
	name = "Nanotrasen-brand microwave"
	desc = "The famous Nanotrasen-brand microwave, the multi-purpose cooking appliance every station needs! This one appears to be drawn onto a cardboard box."
	icon = 'icons/obj/kitchen.dmi'
	icon_state = "mw"

/obj/structure/showcase/machinery/cloning_pod
	name = "cloning pod exhibit"
	desc = "Signs describe how cloning pods like these ensure that every Nanotrasen employee can carry out their contracts in full, even in the unlikely event of their catastrophic death. Hopefully they aren't all made of cardboard, like this one."
	icon = 'icons/obj/machines/cloning.dmi'
	icon_state = "pod_0"

/obj/structure/showcase/perfect_employee
	name = "'Perfect Man' employee exhibit"
	desc = "A stand with a model of the perfect Nanotrasen Employee bolted to it. Signs indicate it is robustly genetically engineered, as well as being ruthlessly loyal."

/obj/structure/showcase/machinery/tv
	name = "Nanotrasen corporate newsfeed"
	desc = "A slightly battered looking TV. Various Nanotrasen infomercials play on a loop, accompanied by a jaunty tune."
	icon = 'icons/obj/computer.dmi'
	icon_state = "television"

/obj/structure/showcase/machinery/signal_decrypter
	name = "subsystem signal decrypter"
	desc = "A strange machine that's supposedly used to help pick up and decrypt wave signals."
	icon = 'icons/obj/machines/telecomms.dmi'
	icon_state = "processor"



//Deconstructing
//Showcases can be any sprite, so it makes sense that they can't be constructed.
//However if a player wants to move an existing showcase or remove one, this is for that.

/obj/structure/showcase/attackby(obj/item/W, mob/user)
	if(W.tool_behaviour == TOOL_SCREWDRIVER && !anchored)
		if(deconstruction_state == SHOWCASE_SCREWDRIVERED)
			to_chat(user, "<span class='notice'>You screw the screws back into the showcase.</span>")
			W.play_tool_sound(src, 100)
			deconstruction_state = SHOWCASE_CONSTRUCTED
		else if (deconstruction_state == SHOWCASE_CONSTRUCTED)
			to_chat(user, "<span class='notice'>You unscrew the screws.</span>")
			W.play_tool_sound(src, 100)
			deconstruction_state = SHOWCASE_SCREWDRIVERED

	if(W.tool_behaviour == TOOL_CROWBAR && deconstruction_state == SHOWCASE_SCREWDRIVERED)
		if(W.use_tool(src, user, 20, volume=100))
			to_chat(user, "<span class='notice'>You start to crowbar the showcase apart...</span>")
			new /obj/item/stack/sheet/metal(drop_location(), 4)
			qdel(src)

	if(deconstruction_state == SHOWCASE_CONSTRUCTED && default_unfasten_wrench(user, W))
		return

//Feedback is given in examine because showcases can basically have any sprite assigned to them

/obj/structure/showcase/examine(mob/user)
	. = ..()

	switch(deconstruction_state)
		if(SHOWCASE_CONSTRUCTED)
			. += "The showcase is fully constructed."
		if(SHOWCASE_SCREWDRIVERED)
			. += "The showcase has its screws loosened."
		else
			. += "If you see this, something is wrong."
