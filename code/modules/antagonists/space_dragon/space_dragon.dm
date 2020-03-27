/datum/antagonist/space_dragon
	name = "Space Dragon"
	show_in_antagpanel = FALSE
	show_name_in_check_antagonists = TRUE

/datum/antagonist/space_dragon/greet()
	to_chat(owner, "<b>I am Space Dragon, ex-space carp, and defender of the secrets of constellation, Draco.\n\
					Fabulous secret powers were revealed to me the day I held aloft a wizard's staff of change and said 'By the power of Draco, I have the power!'\n\
					The wizard was turned into the short-lived Pastry Cat while I became Space Dragon, the most powerful beast in the universe.\n\
					Clicking a tile will shoot fire onto that tile.\n\
					Using Tail Sweep will let me get the better of those who come too close.\n\
					Attacking dead bodies will allow me to gib them to restore health.\n\
					From the wizard's writings, he had been studying this station and its hierarchy. From this, I know who leads the station, and will kill them so the station underlings see me as their new leader.</b>")
	owner.announce_objectives()
	SEND_SOUND(owner.current, sound('sound/magic/demon_attack1.ogg'))

/datum/antagonist/space_dragon/proc/forge_objectives()
	var/current_heads = SSjob.get_all_heads()
	var/datum/objective/assassinate/killchosen = new
	killchosen.owner = owner
	var/datum/mind/selected = pick(current_heads)
	killchosen.target = selected
	killchosen.update_explanation_text()
	objectives += killchosen
	var/datum/objective/survive/survival = new
	survival.owner = owner
	objectives += survival

/datum/antagonist/space_dragon/on_gain()
	forge_objectives()
	. = ..()
