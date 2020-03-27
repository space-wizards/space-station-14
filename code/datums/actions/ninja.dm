/datum/action/item_action/initialize_ninja_suit
	name = "Toggle ninja suit"

/datum/action/item_action/ninjasmoke
	name = "Smoke Bomb"
	desc = "Blind your enemies momentarily with a well-placed smoke bomb."
	button_icon_state = "smoke"
	icon_icon = 'icons/mob/actions/actions_spells.dmi'

/datum/action/item_action/ninjaboost
	check_flags = NONE
	name = "Adrenaline Boost"
	desc = "Inject a secret chemical that will counteract all movement-impairing effect."
	button_icon_state = "repulse"
	icon_icon = 'icons/mob/actions/actions_spells.dmi'

/datum/action/item_action/ninjapulse
	name = "EM Burst (25E)"
	desc = "Disable any nearby technology with an electro-magnetic pulse."
	button_icon_state = "emp"
	icon_icon = 'icons/mob/actions/actions_spells.dmi'

/datum/action/item_action/ninjastar
	name = "Create Throwing Stars (1E)"
	desc = "Creates some throwing stars"
	button_icon_state = "throwingstar"
	icon_icon = 'icons/obj/items_and_weapons.dmi'

/datum/action/item_action/ninjanet
	name = "Energy Net (20E)"
	desc = "Captures a fallen opponent in a net of energy. Will teleport them to a holding facility after 30 seconds."
	button_icon_state = "energynet"
	icon_icon = 'icons/effects/effects.dmi'

/datum/action/item_action/ninja_sword_recall
	name = "Recall Energy Katana (Variable Cost)"
	desc = "Teleports the Energy Katana linked to this suit to its wearer, cost based on distance."
	button_icon_state = "energy_katana"
	icon_icon = 'icons/obj/items_and_weapons.dmi'

/datum/action/item_action/ninja_stealth
	name = "Toggle Stealth"
	desc = "Toggles stealth mode on and off."
	button_icon_state = "ninja_cloak"
	icon_icon = 'icons/mob/actions/actions_minor_antag.dmi'

/datum/action/item_action/toggle_glove
	name = "Toggle interaction"
	desc = "Switch between normal interaction and drain mode."
	button_icon_state = "s-ninjan"
	icon_icon = 'icons/obj/clothing/gloves.dmi'
