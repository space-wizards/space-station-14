//adds godmode while in the container, prevents moving, and clears these effects up after leaving the stone
/datum/component/soulstoned
	var/atom/movable/container

/datum/component/soulstoned/Initialize(atom/movable/container)
	if(!isanimal(parent))
		return COMPONENT_INCOMPATIBLE
	var/mob/living/simple_animal/S = parent

	src.container = container

	S.forceMove(container)

	S.status_flags |= GODMODE
	S.mobility_flags = NONE
	S.health = S.maxHealth
	S.bruteloss = 0

	RegisterSignal(S, COMSIG_MOVABLE_MOVED, .proc/free_prisoner)

/datum/component/soulstoned/proc/free_prisoner()
	var/mob/living/simple_animal/S = parent
	if(S.loc != container)
		qdel(src)

/datum/component/soulstoned/UnregisterFromParent()
	var/mob/living/simple_animal/S = parent
	S.status_flags &= ~GODMODE
	S.mobility_flags = MOBILITY_FLAGS_DEFAULT

  
