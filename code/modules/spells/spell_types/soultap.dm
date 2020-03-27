#define HEALTH_LOST_PER_SOUL_TAP 20

//SOUL TAP!//
//Trades 20 max health for a refresh on all of your spells. I was considering making it depend on the cooldowns of your spells, but I want to support "Big spell wizard" with this loadout.
//the two spells that sound most problematic with this is mindswap and lichdom, but soul tap requires clothes for mindswap and lichdom takes your soul.

/obj/effect/proc_holder/spell/self/tap
	name = "Soul Tap"
	desc = "Fuel your spells using your own soul!"
	school = "necromancy" //i could see why this wouldn't be necromancy but messing with souls or whatever. ectomancy?
	charge_max = 10
	invocation = "AT ANY COST!"
	invocation_type = "shout"
	level_max = 0
	cooldown_min = 10

	action_icon = 'icons/mob/actions/actions_spells.dmi'
	action_icon_state = "soultap"

/obj/effect/proc_holder/spell/self/tap/cast(mob/living/user = usr)
	if(!user.mind.hasSoul)
		to_chat(user, "<span class='warning'>You do not possess a soul to tap into!</span>")
		return
	to_chat(user, "<span class='danger'>Your body feels drained and there is a burning pain in your chest.</span>")
	user.maxHealth -= HEALTH_LOST_PER_SOUL_TAP
	user.health = min(user.health, user.maxHealth)
	if(user.maxHealth <= 0)
		to_chat(user, "<span class='userdanger'>Your weakened soul is completely consumed by the tap!</span>")
		user.mind.hasSoul = FALSE
	for(var/obj/effect/proc_holder/spell/spell in user.mind.spell_list)
		spell.charge_counter = spell.charge_max
		spell.recharging = FALSE
		spell.update_icon()
