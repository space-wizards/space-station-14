#define TARGET_CLOSEST 1
#define TARGET_RANDOM 2


/obj/effect/proc_holder
	var/panel = "Debug"//What panel the proc holder needs to go on.
	var/active = FALSE //Used by toggle based abilities.
	var/ranged_mousepointer
	var/mob/living/ranged_ability_user
	var/ranged_clickcd_override = -1
	var/has_action = TRUE
	var/datum/action/spell_action/action = null
	var/action_icon = 'icons/mob/actions/actions_spells.dmi'
	var/action_icon_state = "spell_default"
	var/action_background_icon_state = "bg_spell"
	var/base_action = /datum/action/spell_action

/obj/effect/proc_holder/Initialize()
	. = ..()
	if(has_action)
		action = new base_action(src)

/obj/effect/proc_holder/proc/on_gain(mob/living/user)
	return

/obj/effect/proc_holder/proc/on_lose(mob/living/user)
	return

/obj/effect/proc_holder/proc/fire(mob/living/user)
	return TRUE

/obj/effect/proc_holder/proc/get_panel_text()
	return ""

GLOBAL_LIST_INIT(spells, typesof(/obj/effect/proc_holder/spell)) //needed for the badmin verb for now

/obj/effect/proc_holder/Destroy()
	if (action)
		qdel(action)
	if(ranged_ability_user)
		remove_ranged_ability()
	return ..()

/obj/effect/proc_holder/singularity_act()
	return

/obj/effect/proc_holder/singularity_pull()
	return

/obj/effect/proc_holder/proc/InterceptClickOn(mob/living/caller, params, atom/A)
	if(caller.ranged_ability != src || ranged_ability_user != caller) //I'm not actually sure how these would trigger, but, uh, safety, I guess?
		to_chat(caller, "<span class='warning'><b>[caller.ranged_ability.name]</b> has been disabled.</span>")
		caller.ranged_ability.remove_ranged_ability()
		return TRUE //TRUE for failed, FALSE for passed.
	if(ranged_clickcd_override >= 0)
		ranged_ability_user.next_click = world.time + ranged_clickcd_override
	else
		ranged_ability_user.next_click = world.time + CLICK_CD_CLICK_ABILITY
	ranged_ability_user.face_atom(A)
	return FALSE

/obj/effect/proc_holder/proc/add_ranged_ability(mob/living/user, msg, forced)
	if(!user || !user.client)
		return
	if(user.ranged_ability && user.ranged_ability != src)
		if(forced)
			to_chat(user, "<span class='warning'><b>[user.ranged_ability.name]</b> has been replaced by <b>[name]</b>.</span>")
			user.ranged_ability.remove_ranged_ability()
		else
			return
	user.ranged_ability = src
	user.click_intercept = src
	user.update_mouse_pointer()
	ranged_ability_user = user
	if(msg)
		to_chat(ranged_ability_user, msg)
	active = TRUE
	update_icon()

/obj/effect/proc_holder/proc/remove_ranged_ability(msg)
	if(!ranged_ability_user || !ranged_ability_user.client || (ranged_ability_user.ranged_ability && ranged_ability_user.ranged_ability != src)) //To avoid removing the wrong ability
		return
	ranged_ability_user.ranged_ability = null
	ranged_ability_user.click_intercept = null
	ranged_ability_user.update_mouse_pointer()
	if(msg)
		to_chat(ranged_ability_user, msg)
	ranged_ability_user = null
	active = FALSE
	update_icon()

/obj/effect/proc_holder/spell
	name = "Spell"
	desc = "A wizard spell."
	panel = "Spells"
	var/sound = null //The sound the spell makes when it is cast
	anchored = TRUE // Crap like fireball projectiles are proc_holders, this is needed so fireballs don't get blown back into your face via atmos etc.
	pass_flags = PASSTABLE
	density = FALSE
	opacity = 0

	var/school = "evocation" //not relevant at now, but may be important later if there are changes to how spells work. the ones I used for now will probably be changed... maybe spell presets? lacking flexibility but with some other benefit?

	var/charge_type = "recharge" //can be recharge or charges, see charge_max and charge_counter descriptions; can also be based on the holder's vars now, use "holder_var" for that

	var/charge_max = 100 //recharge time in deciseconds if charge_type = "recharge" or starting charges if charge_type = "charges"
	var/charge_counter = 0 //can only cast spells if it equals recharge, ++ each decisecond if charge_type = "recharge" or -- each cast if charge_type = "charges"
	var/still_recharging_msg = "<span class='notice'>The spell is still recharging.</span>"
	var/recharging = TRUE

	var/holder_var_type = "bruteloss" //only used if charge_type equals to "holder_var"
	var/holder_var_amount = 20 //same. The amount adjusted with the mob's var when the spell is used

	var/clothes_req = TRUE //see if it requires clothes
	var/cult_req = FALSE //SPECIAL SNOWFLAKE clothes required for cult only spells
	var/human_req = FALSE //spell can only be cast by humans
	var/nonabstract_req = FALSE //spell can only be cast by mobs that are physical entities
	var/stat_allowed = FALSE //see if it requires being conscious/alive, need to set to 1 for ghostpells
	var/phase_allowed = FALSE // If true, the spell can be cast while phased, eg. blood crawling, ethereal jaunting
	var/antimagic_allowed = FALSE // If false, the spell cannot be cast while under the effect of antimagic
	var/invocation = "HURP DURP" //what is uttered when the wizard casts the spell
	var/invocation_emote_self = null
	var/invocation_type = "none" //can be none, whisper, emote and shout
	var/range = 7 //the range of the spell; outer radius for aoe spells
	var/message = "" //whatever it says to the guy affected by it
	var/selection_type = "view" //can be "range" or "view"
	var/spell_level = 0 //if a spell can be taken multiple times, this raises
	var/level_max = 4 //The max possible level_max is 4
	var/cooldown_min = 0 //This defines what spell quickened four times has as a cooldown. Make sure to set this for every spell
	var/player_lock = TRUE //If it can be used by simple mobs

	var/overlay = 0
	var/overlay_icon = 'icons/obj/wizard.dmi'
	var/overlay_icon_state = "spell"
	var/overlay_lifespan = 0

	var/sparks_spread = 0
	var/sparks_amt = 0 //cropped at 10
	var/smoke_spread = 0 //1 - harmless, 2 - harmful
	var/smoke_amt = 0 //cropped at 10

	var/centcom_cancast = TRUE //Whether or not the spell should be allowed on z2

	action_icon = 'icons/mob/actions/actions_spells.dmi'
	action_icon_state = "spell_default"
	action_background_icon_state = "bg_spell"
	base_action = /datum/action/spell_action/spell

/obj/effect/proc_holder/spell/proc/cast_check(skipcharge = 0,mob/user = usr) //checks if the spell can be cast based on its settings; skipcharge is used when an additional cast_check is called inside the spell
	if(player_lock)
		if(!user.mind || !(src in user.mind.spell_list) && !(src in user.mob_spell_list))
			to_chat(user, "<span class='warning'>You shouldn't have this spell! Something's wrong.</span>")
			return FALSE
	else
		if(!(src in user.mob_spell_list))
			return FALSE

	var/turf/T = get_turf(user)
	if(is_centcom_level(T.z) && !centcom_cancast) //Certain spells are not allowed on the centcom zlevel
		to_chat(user, "<span class='warning'>You can't cast this spell here!</span>")
		return FALSE

	if(!skipcharge)
		if(!charge_check(user))
			return FALSE

	if(user.stat && !stat_allowed)
		to_chat(user, "<span class='warning'>Not when you're incapacitated!</span>")
		return FALSE

	if(!antimagic_allowed)
		var/antimagic = user.anti_magic_check(TRUE, FALSE, FALSE, 0, TRUE)
		if(antimagic)
			if(isitem(antimagic))
				to_chat(user, "<span class='notice'>[antimagic] is interfering with your magic.</span>")
			else
				to_chat(user, "<span class='warning'>Magic seems to flee from you, you can't gather enough power to cast this spell.</span>")
			return FALSE

	if(!phase_allowed && istype(user.loc, /obj/effect/dummy))
		to_chat(user, "<span class='warning'>[name] cannot be cast unless you are completely manifested in the material plane!</span>")
		return FALSE

	if(ishuman(user))

		var/mob/living/carbon/human/H = user

		if((invocation_type == "whisper" || invocation_type == "shout") && !H.can_speak_vocal())
			to_chat(user, "<span class='warning'>You can't get the words out!</span>")
			return FALSE

		var/list/casting_clothes = typecacheof(list(/obj/item/clothing/suit/wizrobe,
		/obj/item/clothing/suit/space/hardsuit/wizard,
		/obj/item/clothing/head/wizard,
		/obj/item/clothing/head/helmet/space/hardsuit/wizard,
		/obj/item/clothing/suit/space/hardsuit/shielded/wizard,
		/obj/item/clothing/head/helmet/space/hardsuit/shielded/wizard))

		if(clothes_req) //clothes check
			if(!is_type_in_typecache(H.wear_suit, casting_clothes))
				to_chat(H, "<span class='warning'>You don't feel strong enough without your robe!</span>")
				return FALSE
			if(!is_type_in_typecache(H.head, casting_clothes))
				to_chat(H, "<span class='warning'>You don't feel strong enough without your hat!</span>")
				return FALSE
		if(cult_req) //CULT_REQ CLOTHES CHECK
			if(!istype(H.wear_suit, /obj/item/clothing/suit/magusred) && !istype(H.wear_suit, /obj/item/clothing/suit/space/hardsuit/cult))
				to_chat(H, "<span class='warning'>You don't feel strong enough without your armor.</span>")
				return FALSE
			if(!istype(H.head, /obj/item/clothing/head/magus) && !istype(H.head, /obj/item/clothing/head/helmet/space/hardsuit/cult))
				to_chat(H, "<span class='warning'>You don't feel strong enough without your helmet.</span>")
				return FALSE
	else
		if(clothes_req || human_req)
			to_chat(user, "<span class='warning'>This spell can only be cast by humans!</span>")
			return FALSE
		if(nonabstract_req && (isbrain(user) || ispAI(user)))
			to_chat(user, "<span class='warning'>This spell can only be cast by physical beings!</span>")
			return FALSE


	if(!skipcharge)
		switch(charge_type)
			if("recharge")
				charge_counter = 0 //doesn't start recharging until the targets selecting ends
			if("charges")
				charge_counter-- //returns the charge if the targets selecting fails
			if("holdervar")
				adjust_var(user, holder_var_type, holder_var_amount)
	if(action)
		action.UpdateButtonIcon()
	return TRUE

/obj/effect/proc_holder/spell/proc/charge_check(mob/user, silent = FALSE)
	switch(charge_type)
		if("recharge")
			if(charge_counter < charge_max)
				if(!silent)
					to_chat(user, still_recharging_msg)
				return FALSE
		if("charges")
			if(!charge_counter)
				if(!silent)
					to_chat(user, "<span class='warning'>[name] has no charges left!</span>")
				return FALSE
	return TRUE

/obj/effect/proc_holder/spell/proc/invocation(mob/user = usr) //spelling the spell out and setting it on recharge/reducing charges amount
	switch(invocation_type)
		if("shout")
			if(prob(50))//Auto-mute? Fuck that noise
				user.say(invocation, forced = "spell")
			else
				user.say(replacetext(invocation," ","`"), forced = "spell")
		if("whisper")
			if(prob(50))
				user.whisper(invocation)
			else
				user.whisper(replacetext(invocation," ","`"))
		if("emote")
			user.visible_message(invocation, invocation_emote_self) //same style as in mob/living/emote.dm

/obj/effect/proc_holder/spell/proc/playMagSound()
	playsound(get_turf(usr), sound,50,TRUE)

/obj/effect/proc_holder/spell/Initialize()
	. = ..()
	START_PROCESSING(SSfastprocess, src)

	still_recharging_msg = "<span class='warning'>[name] is still recharging!</span>"
	charge_counter = charge_max

/obj/effect/proc_holder/spell/Destroy()
	STOP_PROCESSING(SSfastprocess, src)
	qdel(action)
	return ..()

/obj/effect/proc_holder/spell/Click()
	if(cast_check())
		choose_targets()
	return 1

/obj/effect/proc_holder/spell/proc/choose_targets(mob/user = usr) //depends on subtype - /targeted or /aoe_turf
	return

/obj/effect/proc_holder/spell/proc/can_target(mob/living/target)
	return TRUE

/obj/effect/proc_holder/spell/proc/start_recharge()
	recharging = TRUE

/obj/effect/proc_holder/spell/process()
	if(recharging && charge_type == "recharge" && (charge_counter < charge_max))
		charge_counter += 2	//processes 5 times per second instead of 10.
		if(charge_counter >= charge_max)
			action.UpdateButtonIcon()
			charge_counter = charge_max
			recharging = FALSE

/obj/effect/proc_holder/spell/proc/perform(list/targets, recharge = TRUE, mob/user = usr) //if recharge is started is important for the trigger spells
	before_cast(targets)
	invocation(user)
	if(user && user.ckey)
		user.log_message("<span class='danger'>cast the spell [name].</span>", LOG_ATTACK)
	if(recharge)
		recharging = TRUE
	if(sound)
		playMagSound()
	cast(targets,user=user)
	after_cast(targets)
	if(action)
		action.UpdateButtonIcon()

/obj/effect/proc_holder/spell/proc/before_cast(list/targets)
	if(overlay)
		for(var/atom/target in targets)
			var/location
			if(isliving(target))
				location = target.loc
			else if(isturf(target))
				location = target
			var/obj/effect/overlay/spell = new /obj/effect/overlay(location)
			spell.icon = overlay_icon
			spell.icon_state = overlay_icon_state
			spell.anchored = TRUE
			spell.density = FALSE
			QDEL_IN(spell, overlay_lifespan)

/obj/effect/proc_holder/spell/proc/after_cast(list/targets)
	for(var/atom/target in targets)
		var/location
		if(isliving(target))
			location = target.loc
		else if(isturf(target))
			location = target
		if(isliving(target) && message)
			to_chat(target, text("[message]"))
		if(sparks_spread)
			do_sparks(sparks_amt, FALSE, location)
		if(smoke_spread)
			if(smoke_spread == 1)
				var/datum/effect_system/smoke_spread/smoke = new
				smoke.set_up(smoke_amt, location)
				smoke.start()
			else if(smoke_spread == 2)
				var/datum/effect_system/smoke_spread/bad/smoke = new
				smoke.set_up(smoke_amt, location)
				smoke.start()
			else if(smoke_spread == 3)
				var/datum/effect_system/smoke_spread/sleeping/smoke = new
				smoke.set_up(smoke_amt, location)
				smoke.start()


/obj/effect/proc_holder/spell/proc/cast(list/targets,mob/user = usr)
	return

/obj/effect/proc_holder/spell/proc/view_or_range(distance = world.view, center=usr, type="view")
	switch(type)
		if("view")
			. = view(distance,center)
		if("range")
			. = range(distance,center)

/obj/effect/proc_holder/spell/proc/revert_cast(mob/user = usr) //resets recharge or readds a charge
	switch(charge_type)
		if("recharge")
			charge_counter = charge_max
		if("charges")
			charge_counter++
		if("holdervar")
			adjust_var(user, holder_var_type, -holder_var_amount)
	if(action)
		action.UpdateButtonIcon()

/obj/effect/proc_holder/spell/proc/adjust_var(mob/living/target = usr, type, amount) //handles the adjustment of the var when the spell is used. has some hardcoded types
	if (!istype(target))
		return
	switch(type)
		if("bruteloss")
			target.adjustBruteLoss(amount)
		if("fireloss")
			target.adjustFireLoss(amount)
		if("toxloss")
			target.adjustToxLoss(amount)
		if("oxyloss")
			target.adjustOxyLoss(amount)
		if("stun")
			target.AdjustStun(amount)
		if("knockdown")
			target.AdjustKnockdown(amount)
		if("paralyze")
			target.AdjustParalyzed(amount)
		if("immobilize")
			target.AdjustImmobilized(amount)
		if("unconscious")
			target.AdjustUnconscious(amount)
		else
			target.vars[type] += amount //I bear no responsibility for the runtimes that'll happen if you try to adjust non-numeric or even non-existent vars

/obj/effect/proc_holder/spell/targeted //can mean aoe for mobs (limited/unlimited number) or one target mob
	var/max_targets = 1 //leave 0 for unlimited targets in range, 1 for one selectable target in range, more for limited number of casts (can all target one guy, depends on target_ignore_prev) in range
	var/target_ignore_prev = 1 //only important if max_targets > 1, affects if the spell can be cast multiple times at one person from one cast
	var/include_user = 0 //if it includes usr in the target list
	var/random_target = 0 // chooses random viable target instead of asking the caster
	var/random_target_priority = TARGET_CLOSEST // if random_target is enabled how it will pick the target


/obj/effect/proc_holder/spell/aoe_turf //affects all turfs in view or range (depends)
	var/inner_radius = -1 //for all your ring spell needs

/obj/effect/proc_holder/spell/targeted/choose_targets(mob/user = usr)
	var/list/targets = list()

	switch(max_targets)
		if(0) //unlimited
			for(var/mob/living/target in view_or_range(range, user, selection_type))
				if(!can_target(target))
					continue
				targets += target
		if(1) //single target can be picked
			if(range < 0)
				targets += user
			else
				var/possible_targets = list()

				for(var/mob/living/M in view_or_range(range, user, selection_type))
					if(!include_user && user == M)
						continue
					if(!can_target(M))
						continue
					possible_targets += M

				//targets += input("Choose the target for the spell.", "Targeting") as mob in possible_targets
				//Adds a safety check post-input to make sure those targets are actually in range.
				var/mob/M
				if(!random_target)
					M = input("Choose the target for the spell.", "Targeting") as null|mob in sortNames(possible_targets)
				else
					switch(random_target_priority)
						if(TARGET_RANDOM)
							M = pick(possible_targets)
						if(TARGET_CLOSEST)
							for(var/mob/living/L in possible_targets)
								if(M)
									if(get_dist(user,L) < get_dist(user,M))
										if(los_check(user,L))
											M = L
								else
									if(los_check(user,L))
										M = L
				if(M in view_or_range(range, user, selection_type))
					targets += M

		else
			var/list/possible_targets = list()
			for(var/mob/living/target in view_or_range(range, user, selection_type))
				if(!can_target(target))
					continue
				possible_targets += target
			for(var/i=1,i<=max_targets,i++)
				if(!possible_targets.len)
					break
				if(target_ignore_prev)
					var/target = pick(possible_targets)
					possible_targets -= target
					targets += target
				else
					targets += pick(possible_targets)

	if(!include_user && (user in targets))
		targets -= user

	if(!targets.len) //doesn't waste the spell
		revert_cast(user)
		return

	perform(targets,user=user)
/obj/effect/proc_holder/spell/aoe_turf/choose_targets(mob/user = usr)
	var/list/targets = list()

	for(var/turf/target in view_or_range(range,user,selection_type))
		if(!can_target(target))
			continue
		if(!(target in view_or_range(inner_radius,user,selection_type)))
			targets += target

	if(!targets.len) //doesn't waste the spell
		revert_cast()
		return

	perform(targets,user=user)

/obj/effect/proc_holder/spell/proc/updateButtonIcon(status_only, force)
	action.UpdateButtonIcon(status_only, force)

/obj/effect/proc_holder/spell/proc/can_be_cast_by(mob/caster)
	if((human_req || clothes_req) && !ishuman(caster))
		return 0
	return 1

/obj/effect/proc_holder/spell/targeted/proc/los_check(mob/A,mob/B)
	//Checks for obstacles from A to B
	var/obj/dummy = new(A.loc)
	dummy.pass_flags |= PASSTABLE
	for(var/turf/turf in getline(A,B))
		for(var/atom/movable/AM in turf)
			if(!AM.CanPass(dummy,turf,1))
				qdel(dummy)
				return 0
	qdel(dummy)
	return 1

/obj/effect/proc_holder/spell/proc/can_cast(mob/user = usr)
	if(((!user.mind) || !(src in user.mind.spell_list)) && !(src in user.mob_spell_list))
		return FALSE

	if(!charge_check(user,TRUE))
		return FALSE

	if(user.stat && !stat_allowed)
		return FALSE

	if(!antimagic_allowed && user.anti_magic_check(TRUE, FALSE, FALSE, 0, TRUE))
		return FALSE

	if(!ishuman(user))
		if(clothes_req || human_req)
			return FALSE
		if(nonabstract_req && (isbrain(user) || ispAI(user)))
			return FALSE
	return TRUE

/obj/effect/proc_holder/spell/self //Targets only the caster. Good for buffs and heals, but probably not wise for fireballs (although they usually fireball themselves anyway, honke)
	range = -1 //Duh

/obj/effect/proc_holder/spell/self/choose_targets(mob/user = usr)
	if(!user)
		revert_cast()
		return
	perform(null,user=user)

/obj/effect/proc_holder/spell/self/basic_heal //This spell exists mainly for debugging purposes, and also to show how casting works
	name = "Lesser Heal"
	desc = "Heals a small amount of brute and burn damage."
	human_req = TRUE
	clothes_req = FALSE
	charge_max = 100
	cooldown_min = 50
	invocation = "Victus sano!"
	invocation_type = "whisper"
	school = "restoration"
	sound = 'sound/magic/staff_healing.ogg'

/obj/effect/proc_holder/spell/self/basic_heal/cast(mob/living/carbon/human/user) //Note the lack of "list/targets" here. Instead, use a "user" var depending on mob requirements.
	//Also, notice the lack of a "for()" statement that looks through the targets. This is, again, because the spell can only have a single target.
	user.visible_message("<span class='warning'>A wreath of gentle light passes over [user]!</span>", "<span class='notice'>You wreath yourself in healing light!</span>")
	user.adjustBruteLoss(-10)
	user.adjustFireLoss(-10)
