#define WIRE_RECEIVE		(1<<0)
#define WIRE_PULSE			(1<<1)
#define WIRE_PULSE_SPECIAL	(1<<2)
#define WIRE_RADIO_RECEIVE	(1<<3)
#define WIRE_RADIO_PULSE	(1<<4)
#define ASSEMBLY_BEEP_VOLUME 5

/obj/item/assembly
	name = "assembly"
	desc = "A small electronic device that should never exist."
	icon = 'icons/obj/assemblies/new_assemblies.dmi'
	icon_state = ""
	flags_1 = CONDUCT_1
	w_class = WEIGHT_CLASS_SMALL
	custom_materials = list(/datum/material/iron=100)
	throwforce = 2
	throw_speed = 3
	throw_range = 7

	var/is_position_sensitive = FALSE	//set to true if the device has different icons for each position.
										//This will prevent things such as visible lasers from facing the incorrect direction when transformed by assembly_holder's update_icon()
	var/secured = TRUE
	var/list/attached_overlays = null
	var/obj/item/assembly_holder/holder = null
	var/wire_type = WIRE_RECEIVE | WIRE_PULSE
	var/attachable = FALSE // can this be attached to wires
	var/datum/wires/connected = null

	var/next_activate = 0 //When we're next allowed to activate - for spam control
	drop_sound = 'sound/items/handling/component_drop.ogg'
	pickup_sound =  'sound/items/handling/component_pickup.ogg'

/obj/item/assembly/get_part_rating()
	return 1

/obj/item/assembly/proc/on_attach()

/obj/item/assembly/proc/on_detach() //call this when detaching it from a device. handles any special functions that need to be updated ex post facto
	if(!holder)
		return FALSE
	forceMove(holder.drop_location())
	holder = null
	return TRUE

/obj/item/assembly/proc/holder_movement()							//Called when the holder is moved
	if(!holder)
		return FALSE
	setDir(holder.dir)
	return TRUE

/obj/item/assembly/proc/is_secured(mob/user)
	if(!secured)
		to_chat(user, "<span class='warning'>The [name] is unsecured!</span>")
		return FALSE
	return TRUE


//Called when another assembly acts on this one, var/radio will determine where it came from for wire calcs
/obj/item/assembly/proc/pulsed(radio = FALSE)
	if(wire_type & WIRE_RECEIVE)
		INVOKE_ASYNC(src, .proc/activate)
	if(radio && (wire_type & WIRE_RADIO_RECEIVE))
		INVOKE_ASYNC(src, .proc/activate)
	return TRUE


//Called when this device attempts to act on another device, var/radio determines if it was sent via radio or direct
/obj/item/assembly/proc/pulse(radio = FALSE)
	if(connected && wire_type)
		connected.pulse_assembly(src)
		return TRUE
	if(holder && (wire_type & WIRE_PULSE))
		holder.process_activation(src, 1, 0)
	if(holder && (wire_type & WIRE_PULSE_SPECIAL))
		holder.process_activation(src, 0, 1)
	return TRUE


// What the device does when turned on
/obj/item/assembly/proc/activate()
	if(QDELETED(src) || !secured || (next_activate > world.time))
		return FALSE
	next_activate = world.time + 30
	return TRUE


/obj/item/assembly/proc/toggle_secure()
	secured = !secured
	update_icon()
	return secured


/obj/item/assembly/attackby(obj/item/W, mob/user, params)
	if(isassembly(W))
		var/obj/item/assembly/A = W
		if((!A.secured) && (!secured))
			holder = new/obj/item/assembly_holder(get_turf(src))
			holder.assemble(src,A,user)
			to_chat(user, "<span class='notice'>You attach and secure \the [A] to \the [src]!</span>")
		else
			to_chat(user, "<span class='warning'>Both devices must be in attachable mode to be attached together.</span>")
		return
	..()

/obj/item/assembly/screwdriver_act(mob/living/user, obj/item/I)
	if(..())
		return TRUE
	if(toggle_secure())
		to_chat(user, "<span class='notice'>\The [src] is ready!</span>")
	else
		to_chat(user, "<span class='notice'>\The [src] can now be attached!</span>")
	add_fingerprint(user)
	return TRUE

/obj/item/assembly/examine(mob/user)
	. = ..()
	. += "<span class='notice'>\The [src] [secured? "is secured and ready to be used!" : "can be attached to other things."]</span>"


/obj/item/assembly/attack_self(mob/user)
	if(!user)
		return FALSE
	user.set_machine(src)
	interact(user)
	return TRUE

/obj/item/assembly/interact(mob/user)
	return ui_interact(user)
