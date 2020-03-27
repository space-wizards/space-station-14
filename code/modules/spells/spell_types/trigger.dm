/obj/effect/proc_holder/spell/targeted/trigger
	name = "Trigger"
	desc = "This spell triggers another spell or a few."

	var/list/linked_spells = list() //those are just referenced by the trigger spell and are unaffected by it directly
	var/list/starting_spells = list() //those are added on New() to contents from default spells and are deleted when the trigger spell is deleted to prevent memory leaks

/obj/effect/proc_holder/spell/targeted/trigger/Initialize()
	. = ..()

	for(var/spell in starting_spells)
		var/spell_to_add = text2path(spell)
		new spell_to_add(src) //should result in adding to contents, needs testing

/obj/effect/proc_holder/spell/targeted/trigger/Destroy()
	for(var/spell in contents)
		qdel(spell)
	linked_spells = null
	starting_spells = null
	return ..()

/obj/effect/proc_holder/spell/targeted/trigger/cast(list/targets,mob/user = usr)
	playMagSound()
	for(var/mob/living/target in targets)
		for(var/obj/effect/proc_holder/spell/spell in contents)
			spell.perform(list(target),0)
		for(var/obj/effect/proc_holder/spell/spell in linked_spells)
			spell.perform(list(target),0)

	return
