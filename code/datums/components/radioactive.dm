#define RAD_AMOUNT_LOW 50
#define RAD_AMOUNT_MEDIUM 200
#define RAD_AMOUNT_HIGH 500
#define RAD_AMOUNT_EXTREME 1000

/datum/component/radioactive
	dupe_mode = COMPONENT_DUPE_UNIQUE_PASSARGS

	var/source

	var/hl3_release_date //the half-life measured in ticks
	var/strength
	var/can_contaminate

/datum/component/radioactive/Initialize(_strength=0, _source, _half_life=RAD_HALF_LIFE, _can_contaminate=TRUE)
	strength = _strength
	source = _source
	hl3_release_date = _half_life
	can_contaminate = _can_contaminate

	if(istype(parent, /atom)) 
		RegisterSignal(parent, COMSIG_PARENT_EXAMINE, .proc/rad_examine)
		if(istype(parent, /obj/item))
			RegisterSignal(parent, COMSIG_ITEM_ATTACK, .proc/rad_attack)
			RegisterSignal(parent, COMSIG_ITEM_ATTACK_OBJ, .proc/rad_attack)
	else
		return COMPONENT_INCOMPATIBLE

	if(strength > RAD_MINIMUM_CONTAMINATION)
		SSradiation.warn(src)

	START_PROCESSING(SSradiation, src)

/datum/component/radioactive/Destroy()
	STOP_PROCESSING(SSradiation, src)
	return ..()

/datum/component/radioactive/process()
	if(!prob(50))
		return
	radiation_pulse(parent, strength, RAD_DISTANCE_COEFFICIENT*2, FALSE, can_contaminate)

	if(!hl3_release_date)
		return
	strength -= strength / hl3_release_date
	if(strength <= RAD_BACKGROUND_RADIATION)
		qdel(src)
		return PROCESS_KILL

/datum/component/radioactive/InheritComponent(datum/component/C, i_am_original, list/arguments)
	if(!i_am_original)
		return
	if(!hl3_release_date) // Permanently radioactive things don't get to grow stronger
		return
	if(C)
		var/datum/component/radioactive/other = C
		strength = max(strength, other.strength)
	else
		strength = max(strength, arguments[1])

/datum/component/radioactive/proc/rad_examine(datum/source, mob/user, atom/thing)
	var/atom/master = parent
	var/list/out = list()
	if(get_dist(master, user) <= 1)
		out += "The air around [master] feels warm"
	switch(strength)
		if(RAD_AMOUNT_LOW to RAD_AMOUNT_MEDIUM)
			out += "[length(out) ? " and it " : "[master] "]feels weird to look at."
		if(RAD_AMOUNT_MEDIUM to RAD_AMOUNT_HIGH)
			out += "[length(out) ? " and it " : "[master] "]seems to be glowing a bit."
		if(RAD_AMOUNT_HIGH to INFINITY) //At this level the object can contaminate other objects
			out += "[length(out) ? " and it " : "[master] "]hurts to look at."
		else
			out += "."
	to_chat(user, out.Join())

/datum/component/radioactive/proc/rad_attack(datum/source, atom/movable/target, mob/living/user)
	radiation_pulse(parent, strength/20)
	target.rad_act(strength/2)
	if(!hl3_release_date)
		return
	strength -= strength / hl3_release_date

#undef RAD_AMOUNT_LOW
#undef RAD_AMOUNT_MEDIUM
#undef RAD_AMOUNT_HIGH
#undef RAD_AMOUNT_EXTREME
