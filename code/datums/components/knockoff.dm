//Items with these will have a chance to get knocked off when disarming
/datum/component/knockoff
	var/knockoff_chance = 100 //Chance to knockoff
	var/list/target_zones //Aiming for these zones will cause the knockoff, null means all zones allowed
	var/list/slots_knockoffable //Can be only knocked off from these slots, null means all slots allowed

/datum/component/knockoff/Initialize(knockoff_chance,zone_override,slots_knockoffable)
	if(!isitem(parent))
		return COMPONENT_INCOMPATIBLE
	RegisterSignal(parent, COMSIG_ITEM_EQUIPPED,.proc/OnEquipped)
	RegisterSignal(parent, COMSIG_ITEM_DROPPED,.proc/OnDropped)

	src.knockoff_chance = knockoff_chance
	
	if(zone_override)
		target_zones = zone_override

	if(slots_knockoffable)
		src.slots_knockoffable = slots_knockoffable

/datum/component/knockoff/proc/Knockoff(mob/living/attacker,zone)
	var/obj/item/I = parent
	var/mob/living/carbon/human/wearer = I.loc
	if(!istype(wearer))
		return
	if(target_zones && !(zone in target_zones))
		return
	if(!prob(knockoff_chance))
		return
	if(!wearer.dropItemToGround(I))
		return

	wearer.visible_message("<span class='warning'>[attacker] knocks off [wearer]'s [I.name]!</span>","<span class='userdanger'>[attacker] knocks off your [I.name]!</span>")

/datum/component/knockoff/proc/OnEquipped(datum/source, mob/living/carbon/human/H,slot)
	if(!istype(H))
		return
	if(slots_knockoffable && !(slot in slots_knockoffable))
		UnregisterSignal(H, COMSIG_HUMAN_DISARM_HIT)
		return
	RegisterSignal(H, COMSIG_HUMAN_DISARM_HIT, .proc/Knockoff, TRUE)

/datum/component/knockoff/proc/OnDropped(datum/source, mob/living/M)
	UnregisterSignal(M, COMSIG_HUMAN_DISARM_HIT)
