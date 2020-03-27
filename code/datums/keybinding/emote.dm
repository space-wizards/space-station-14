/datum/keybinding/emote
	category = CATEGORY_EMOTE
	weight = WEIGHT_EMOTE
	var/emote_key

/datum/keybinding/emote/proc/link_to_emote(datum/emote/faketype)
	hotkey_keys = list("Unbound")
	emote_key = initial(faketype.key)
	name = initial(faketype.key)
	full_name = capitalize(initial(faketype.key))
	description = "Do the emote '*[emote_key]'"

/datum/keybinding/emote/down(client/user)
	. = ..()
	return user.mob.emote(emote_key, intentional=TRUE)
