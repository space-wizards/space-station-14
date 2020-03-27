/datum/component/bane
	dupe_mode = COMPONENT_DUPE_ALLOWED

	var/mobtype
	var/speciestype
	var/damage_multiplier

/datum/component/bane/Initialize(mobtype, damage_multiplier=1)
	if(!isitem(parent))
		return COMPONENT_INCOMPATIBLE

	if(ispath(mobtype, /mob/living))
		src.mobtype = mobtype
	else if(ispath(mobtype, /datum/species))
		speciestype = mobtype
	else
		return COMPONENT_INCOMPATIBLE

	src.damage_multiplier = damage_multiplier

/datum/component/bane/RegisterWithParent()
	if(speciestype)
		RegisterSignal(parent, COMSIG_ITEM_AFTERATTACK, .proc/speciesCheck)
	else
		RegisterSignal(parent, COMSIG_ITEM_AFTERATTACK, .proc/mobCheck)

/datum/component/bane/UnregisterFromParent()
	UnregisterSignal(parent, COMSIG_ITEM_AFTERATTACK)

/datum/component/bane/proc/speciesCheck(obj/item/source, atom/target, mob/user, proximity_flag, click_parameters)
	if(!proximity_flag || !is_species(target, speciestype))
		return
	activate(source, target, user)

/datum/component/bane/proc/mobCheck(obj/item/source, atom/target, mob/user, proximity_flag, click_parameters)
	if(!proximity_flag || !istype(target, mobtype))
		return
	activate(source, target, user)

/datum/component/bane/proc/activate(obj/item/source, mob/living/target, mob/attacker)
	if(attacker.a_intent != INTENT_HARM)
		return

	var/extra_damage = max(0, source.force * damage_multiplier)
	target.apply_damage(extra_damage, source.damtype, attacker.zone_selected)
