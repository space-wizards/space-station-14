#define VOTE_TEXT_LIMIT 255
#define MAX_VOTES 255

/obj/structure/votebox
	name = "voting box"
	desc = "A automatic voting box."

	icon = 'icons/obj/votebox.dmi'
	icon_state = "votebox_maint"

	anchored = TRUE

	var/obj/item/card/id/owner //Slapping the box with this ID starts/ends the vote.

	var/voting_active = FALSE //Voting or Maintenance Mode
	var/id_auth = FALSE //One vote per ID.
	var/vote_description = ""

	var/list/voted //List of ID's that already voted.

/obj/structure/votebox/attackby(obj/item/I, mob/living/user, params)
	if(istype(I,/obj/item/card/id))
		if(!owner)
			register_owner(I,user)
			return
	if(istype(I,/obj/item/paper))
		if(voting_active)
			apply_vote(I,user)
		else
			to_chat(user,"<span class='warning'>[src] is in maintenance mode. Voting is not possible at the moment.</span>")
		return
	return ..()

/obj/structure/votebox/interact(mob/user)
	..()
	ui_interact(user)

/obj/structure/votebox/ui_interact(mob/user, ui_key, datum/tgui/ui, force_open, datum/tgui/master_ui, datum/ui_state/state)
	. = ..()

	var/list/dat = list()
	if(!owner)
		dat += "<h1> Unregistered. Swipe ID card to register as voting box operator </h1>"
	dat += "<h1>[vote_description]</h1>"
	if(is_operator(user))
		dat += "Voting: <a href='?src=[REF(src)];act=toggle_vote'>[voting_active ? "Active" : "Maintenance Mode"]</a><br>"
		dat += "Set Description: <a href='?src=[REF(src)];act=set_desc'>Set Description</a><br>"
		dat += "One vote per ID: <a href='?src=[REF(src)];act=toggle_auth'>[id_auth ? "Yes" : "No"]</a><br>"
		dat += "Reset voted ID's: <a href='?src=[REF(src)];act=reset_voted'>Reset</a><br>"
		dat += "Draw random vote: <a href='?src=[REF(src)];act=raffle'>Raffle</a><br>"
		dat += "Shred votes: <a href='?src=[REF(src)];act=shred'>Shred</a><br>"
		dat += "Tally votes: <a href='?src=[REF(src)];act=tally'>Tally</a><br>"

	var/datum/browser/popup = new(user, "votebox", "Voting Box", 300, 300)
	popup.set_content(dat.Join())
	popup.open()

/obj/structure/votebox/Topic(href, href_list)
	if(..())
		return

	var/mob/user = usr
	if(!is_operator(user))
		to_chat(user,"<span class='warning'>Voting box operator authorization required!</span>")
		return

	if(href_list["act"])
		switch(href_list["act"])
			if("toggle_vote")
				voting_active = !voting_active
				update_icon()
			if("toggle_auth")
				id_auth = !id_auth
			if("reset_voted")
				if(voted)
					voted.Cut()
				to_chat(user,"<span class='notice'>You reset the voter buffer. Everyone can vote again.</span>")
			if("raffle")
				raffle(user)
			if("shred")
				shred(user)
			if("tally")
				print_tally(user)
			if("set_desc")
				set_description(user)
		interact(user)

/obj/structure/votebox/proc/register_owner(obj/item/card/id/I,mob/living/user)
	owner = I
	to_chat(user,"<span class='notice'>You register [src] to your ID card.</span>")
	ui_interact(user)

/obj/structure/votebox/proc/set_description(mob/user)
	var/new_description = stripped_multiline_input(user,"Enter new description","Vote Description",vote_description)
	if(new_description)
		vote_description = new_description

/obj/structure/votebox/proc/is_operator(mob/user)
	return user?.get_idcard() == owner

/obj/structure/votebox/proc/apply_vote(obj/item/paper/I,mob/living/user)
	var/obj/item/card/id/voter_card = user.get_idcard()
	if(id_auth)
		if(!voter_card)
			to_chat(user,"<span class='warning'>[src] requires a valid ID card to vote!</span>")
			return
		if(voted && (voter_card in voted))
			to_chat(user,"<span class='warning'>[src] allows only one vote per person.</span>")
			return
	if(user.transferItemToLoc(I,src))
		if(!voted)
			voted = list()
		voted += voter_card
		to_chat(user,"<span class='notice'>You cast your vote.</span>")

/obj/structure/votebox/proc/valid_vote(obj/item/paper/I)
	if(length_char(text) > VOTE_TEXT_LIMIT || findtext(text,"<h1>Voting Results:</h1><hr><ol>"))
		return FALSE
	return TRUE

/obj/structure/votebox/proc/shred(mob/user)
	for(var/obj/item/paper/P in contents)
		qdel(P)
	to_chat(user,"<span class='notice'>You shred the current votes.</span>")

/obj/structure/votebox/wrench_act(mob/living/user, obj/item/I)
	. = ..()
	default_unfasten_wrench(user, I, 40)
	return TRUE

/obj/structure/votebox/crowbar_act(mob/living/user, obj/item/I)
	. = ..()
	if(voting_active)
		to_chat(user,"<span class='warning'>You can only retrieve votes if maintenance mode is active!</span>")
		return FALSE
	dump_contents()
	to_chat(user,"<span class='notice'>You open vote retrieval hatch and dump all the votes.</span>")
	return TRUE

/obj/structure/votebox/dump_contents()
	var/atom/droppoint = drop_location()
	for(var/atom/movable/AM in contents)
		AM.forceMove(droppoint)

/obj/structure/votebox/deconstruct(disassembled)
	dump_contents()
	. = ..()

/obj/structure/votebox/proc/raffle(mob/user)
	var/list/options = list()
	for(var/obj/item/paper/P in contents)
		options += P
	if(!length(options))
		to_chat(user,"<span class='warning>[src] is empty!</span>")
	else
		var/obj/item/paper/P = pick(options)
		user.put_in_hands(P)
		to_chat(user,"<span class='notice'>[src] pops out random vote.</span>")

/obj/structure/votebox/proc/print_tally(mob/user)
	var/list/results = list()
	var/i = 0
	for(var/obj/item/paper/P in contents)
		if(i++ > MAX_VOTES)
			break
		var/text = P.info
		if(!valid_vote(P))
			continue
		if(!results[text])
			results[text] = 1
		else
			results[text] += 1
	sortTim(results, cmp=/proc/cmp_numeric_dsc, associative = TRUE)

	var/obj/item/paper/P = new(drop_location())
	var/list/tally = list()
	tally += "<h1>Voting Results:</h1><hr><ol>"
	for(var/option in results)
		tally += "<li>\"<div class='content'>[option]</div>\" - [results[option]] Vote[results[option] > 1 ? "s" : ""].</li>"
	tally += "</ol>"
	P.extra_headers = {"
	<meta http-equiv='X-UA-Compatible' content='IE=edge'/>
	<style>
		.content{
			max-width:250px;
			display:inline-block;
			overflow:hidden;
			text-overflow:ellipsis;
			white-space:nowrap;
			vertical-align:bottom
		}
		.content br {
			display: none;
		}
		.content hr {
			display: none;
		}
	</style>"}
	P.info = tally.Join()
	P.name = "Voting Results"
	P.update_icon()
	user.put_in_hands(P)
	to_chat(user,"<span class='notice'>[src] prints out the voting tally.</span>")

/obj/structure/votebox/update_icon_state()
	icon_state = "votebox_[voting_active ? "active" : "maint"]"

#undef VOTE_TEXT_LIMIT
#undef MAX_VOTES
