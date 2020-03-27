GLOBAL_LIST_INIT(arcade_prize_pool, list(
		/obj/item/storage/box/snappops = 2,
		/obj/item/toy/talking/AI = 2,
		/obj/item/toy/talking/codex_gigas = 2,
		/obj/item/clothing/under/syndicate/tacticool = 2,
		/obj/item/toy/sword = 2,
		/obj/item/toy/gun = 2,
		/obj/item/gun/ballistic/shotgun/toy/crossbow = 2,
		/obj/item/storage/box/fakesyndiesuit = 2,
		/obj/item/storage/crayons = 2,
		/obj/item/toy/spinningtoy = 2,
		/obj/item/toy/prize/ripley = 1,
		/obj/item/toy/prize/fireripley = 1,
		/obj/item/toy/prize/deathripley = 1,
		/obj/item/toy/prize/gygax = 1,
		/obj/item/toy/prize/durand = 1,
		/obj/item/toy/prize/honk = 1,
		/obj/item/toy/prize/marauder = 1,
		/obj/item/toy/prize/seraph = 1,
		/obj/item/toy/prize/mauler = 1,
		/obj/item/toy/prize/odysseus = 1,
		/obj/item/toy/prize/phazon = 1,
		/obj/item/toy/prize/reticence = 1,
		/obj/item/toy/cards/deck = 2,
		/obj/item/toy/nuke = 2,
		/obj/item/toy/minimeteor = 2,
		/obj/item/toy/redbutton = 2,
		/obj/item/toy/talking/owl = 2,
		/obj/item/toy/talking/griffin = 2,
		/obj/item/coin/antagtoken = 2,
		/obj/item/stack/tile/fakespace/loaded = 2,
		/obj/item/stack/tile/fakepit/loaded = 2,
		/obj/item/toy/toy_xeno = 2,
		/obj/item/storage/box/actionfigure = 1,
		/obj/item/restraints/handcuffs/fake = 2,
		/obj/item/grenade/chem_grenade/glitter/pink = 1,
		/obj/item/grenade/chem_grenade/glitter/blue = 1,
		/obj/item/grenade/chem_grenade/glitter/white = 1,
		/obj/item/toy/eightball = 2,
		/obj/item/toy/windupToolbox = 2,
		/obj/item/toy/clockwork_watch = 2,
		/obj/item/toy/toy_dagger = 2,
		/obj/item/extendohand/acme = 1,
		/obj/item/hot_potato/harmless/toy = 1,
		/obj/item/card/emagfake = 1,
		/obj/item/clothing/shoes/wheelys = 2,
		/obj/item/clothing/shoes/kindleKicks = 2,
		/obj/item/storage/belt/military/snack = 2))

/obj/machinery/computer/arcade
	name = "random arcade"
	desc = "random arcade machine"
	icon_state = "arcade"
	icon_keyboard = null
	icon_screen = "invaders"
	var/list/prize_override
	light_color = LIGHT_COLOR_GREEN

/obj/machinery/computer/arcade/proc/Reset()
	return

/obj/machinery/computer/arcade/Initialize()
	. = ..()
	// If it's a generic arcade machine, pick a random arcade
	// circuit board for it and make the new machine
	if(!circuit)
		var/list/gameodds = list(/obj/item/circuitboard/computer/arcade/battle = 49,
							/obj/item/circuitboard/computer/arcade/orion_trail = 49,
							/obj/item/circuitboard/computer/arcade/amputation = 2)
		var/thegame = pickweight(gameodds)
		var/obj/item/circuitboard/CB = new thegame()
		new CB.build_path(loc, CB)
		return INITIALIZE_HINT_QDEL
	Reset()

/obj/machinery/computer/arcade/proc/prizevend(mob/user)
	SEND_SIGNAL(user, COMSIG_ADD_MOOD_EVENT, "arcade", /datum/mood_event/arcade)
	if(prob(0.0001)) //1 in a million
		new /obj/item/gun/energy/pulse/prize(src)
		visible_message("<span class='notice'>[src] dispenses.. woah, a gun! Way past cool.</span>", "<span class='notice'>You hear a chime and a shot.</span>")
		user.client.give_award(/datum/award/achievement/misc/pulse, user)
		return

	var/prizeselect
	if(prize_override)
		prizeselect = pickweight(prize_override)
	else
		prizeselect = pickweight(GLOB.arcade_prize_pool)
	var/atom/movable/the_prize = new prizeselect(get_turf(src))
	playsound(src, 'sound/machines/machine_vend.ogg', 50, TRUE, extrarange = -3)
	visible_message("<span class='notice'>[src] dispenses [the_prize]!</span>", "<span class='notice'>You hear a chime and a clunk.</span>")

/obj/machinery/computer/arcade/emp_act(severity)
	. = ..()
	var/override = FALSE
	if(prize_override)
		override = TRUE

	if(stat & (NOPOWER|BROKEN) || . & EMP_PROTECT_SELF)
		return

	var/empprize = null
	var/num_of_prizes = 0
	switch(severity)
		if(1)
			num_of_prizes = rand(1,4)
		if(2)
			num_of_prizes = rand(0,2)
	for(var/i = num_of_prizes; i > 0; i--)
		if(override)
			empprize = pickweight(prize_override)
		else
			empprize = pickweight(GLOB.arcade_prize_pool)
		new empprize(loc)
	explosion(loc, -1, 0, 1+num_of_prizes, flame_range = 1+num_of_prizes)

/obj/machinery/computer/arcade/attackby(obj/item/O, mob/user, params)
	if(istype(O, /obj/item/stack/arcadeticket))
		var/obj/item/stack/arcadeticket/T = O
		var/amount = T.get_amount()
		if(amount <2)
			to_chat(user, "<span class='warning'>You need 2 tickets to claim a prize!</span>")
			return
		prizevend(user)
		T.pay_tickets()
		T.update_icon()
		O = T
		to_chat(user, "<span class='notice'>You turn in 2 tickets to the [src] and claim a prize!</span>")
		return

// ** BATTLE ** //


/obj/machinery/computer/arcade/battle
	name = "arcade machine"
	desc = "Does not support Pinball."
	icon_state = "arcade"
	circuit = /obj/item/circuitboard/computer/arcade/battle
	var/enemy_name = "Space Villain"
	var/temp = "Winners don't use space drugs" //Temporary message, for attack messages, etc
	var/player_hp = 30 //Player health/attack points
	var/player_mp = 10
	var/enemy_hp = 45 //Enemy health/attack points
	var/enemy_mp = 20
	var/gameover = FALSE
	var/blocked = FALSE //Player cannot attack/heal while set
	var/turtle = 0
	var/list/weapons = list()

/obj/machinery/computer/arcade/battle/Reset()
	var/name_action
	var/name_part1
	var/name_part2

	if(SSevents.holidays && SSevents.holidays[HALLOWEEN])
		name_action = pick_list(ARCADE_FILE, "rpg_action_halloween")
		name_part1 = pick_list(ARCADE_FILE, "rpg_adjective_halloween")
		name_part2 = pick_list(ARCADE_FILE, "rpg_enemy_halloween")
		weapons = strings(ARCADE_FILE, "rpg_weapon_halloween")
	else if(SSevents.holidays && SSevents.holidays[CHRISTMAS])
		name_action = pick_list(ARCADE_FILE, "rpg_action_xmas")
		name_part1 = pick_list(ARCADE_FILE, "rpg_adjective_xmas")
		name_part2 = pick_list(ARCADE_FILE, "rpg_enemy_xmas")
		weapons = strings(ARCADE_FILE, "rpg_weapon_xmas")
	else if(SSevents.holidays && SSevents.holidays[VALENTINES])
		name_action = pick_list(ARCADE_FILE, "rpg_action_valentines")
		name_part1 = pick_list(ARCADE_FILE, "rpg_adjective_valentines")
		name_part2 = pick_list(ARCADE_FILE, "rpg_enemy_valentines")
		weapons = strings(ARCADE_FILE, "rpg_weapon_valentines")
	else
		name_action = pick_list(ARCADE_FILE, "rpg_action")
		name_part1 = pick_list(ARCADE_FILE, "rpg_adjective")
		name_part2 = pick_list(ARCADE_FILE, "rpg_enemy")
		weapons = strings(ARCADE_FILE, "rpg_weapon")

	enemy_name = ("The " + name_part1 + " " + name_part2)
	name = (name_action + " " + enemy_name)

/obj/machinery/computer/arcade/battle/ui_interact(mob/user)
	. = ..()
	var/dat = "<a href='byond://?src=[REF(src)];close=1'>Close</a>"
	dat += "<center><h4>[enemy_name]</h4></center>"

	dat += "<br><center><h3>[temp]</h3></center>"
	dat += "<br><center>Health: [player_hp] | Magic: [player_mp] | Enemy Health: [enemy_hp]</center>"

	if (gameover)
		dat += "<center><b><a href='byond://?src=[REF(src)];newgame=1'>New Game</a>"
	else
		dat += "<center><b><a href='byond://?src=[REF(src)];attack=1'>Attack</a> | "
		dat += "<a href='byond://?src=[REF(src)];heal=1'>Heal</a> | "
		dat += "<a href='byond://?src=[REF(src)];charge=1'>Recharge Power</a>"

	dat += "</b></center>"
	var/datum/browser/popup = new(user, "arcade", "Space Villain 2000")
	popup.set_content(dat)
	popup.set_title_image(user.browse_rsc_icon(icon, icon_state))
	popup.open()

/obj/machinery/computer/arcade/battle/Topic(href, href_list)
	if(..())
		return

	if (!blocked && !gameover)
		if (href_list["attack"])
			blocked = TRUE
			var/attackamt = rand(2,6)
			var/weapon = pick(weapons)
			temp = "You attack with a [weapon] for [attackamt] damage!"
			playsound(loc, 'sound/arcade/hit.ogg', 50, TRUE, extrarange = -3, falloff = 10)
			updateUsrDialog()
			if(turtle > 0)
				turtle--

			sleep(10)
			enemy_hp -= attackamt
			arcade_action(usr)

		else if (href_list["heal"])
			blocked = TRUE
			var/pointamt = rand(1,3)
			var/healamt = rand(6,8)
			temp = "You use [pointamt] magic to heal for [healamt] damage!"
			playsound(loc, 'sound/arcade/heal.ogg', 50, TRUE, extrarange = -3, falloff = 10)
			updateUsrDialog()
			turtle++

			sleep(10)
			player_mp -= pointamt
			player_hp += healamt
			blocked = TRUE
			updateUsrDialog()
			arcade_action(usr)

		else if (href_list["charge"])
			blocked = TRUE
			var/chargeamt = rand(4,7)
			temp = "You regain [chargeamt] points."
			playsound(loc, 'sound/arcade/mana.ogg', 50, TRUE, extrarange = -3, falloff = 10)
			player_mp += chargeamt
			if(turtle > 0)
				turtle--

			updateUsrDialog()
			sleep(10)
			arcade_action(usr)

	if (href_list["close"])
		usr.unset_machine()
		usr << browse(null, "window=arcade")

	else if (href_list["newgame"]) //Reset everything
		temp = "New Round"
		player_hp = 30
		player_mp = 10
		enemy_hp = 45
		enemy_mp = 20
		gameover = FALSE
		turtle = 0

		if(obj_flags & EMAGGED)
			Reset()
			obj_flags &= ~EMAGGED

	add_fingerprint(usr)
	updateUsrDialog()
	return

/obj/machinery/computer/arcade/battle/proc/arcade_action(mob/user)
	if ((enemy_mp <= 0) || (enemy_hp <= 0))
		if(!gameover)
			gameover = TRUE
			temp = "[enemy_name] has fallen! Rejoice!"
			playsound(loc, 'sound/arcade/win.ogg', 50, TRUE, extrarange = -3, falloff = 10)

			if(obj_flags & EMAGGED)
				new /obj/effect/spawner/newbomb/timer/syndicate(loc)
				new /obj/item/clothing/head/collectable/petehat(loc)
				message_admins("[ADMIN_LOOKUPFLW(usr)] has outbombed Cuban Pete and been awarded a bomb.")
				log_game("[key_name(usr)] has outbombed Cuban Pete and been awarded a bomb.")
				Reset()
				obj_flags &= ~EMAGGED
			else
				prizevend(user)
			SSblackbox.record_feedback("nested tally", "arcade_results", 1, list("win", (obj_flags & EMAGGED ? "emagged":"normal")))


	else if ((obj_flags & EMAGGED) && (turtle >= 4))
		var/boomamt = rand(5,10)
		temp = "[enemy_name] throws a bomb, exploding you for [boomamt] damage!"
		playsound(loc, 'sound/arcade/boom.ogg', 50, TRUE, extrarange = -3, falloff = 10)
		player_hp -= boomamt

	else if ((enemy_mp <= 5) && (prob(70)))
		var/stealamt = rand(2,3)
		temp = "[enemy_name] steals [stealamt] of your power!"
		playsound(loc, 'sound/arcade/steal.ogg', 50, TRUE, extrarange = -3, falloff = 10)
		player_mp -= stealamt
		updateUsrDialog()

		if (player_mp <= 0)
			gameover = TRUE
			sleep(10)
			temp = "You have been drained! GAME OVER"
			playsound(loc, 'sound/arcade/lose.ogg', 50, TRUE, extrarange = -3, falloff = 10)
			if(obj_flags & EMAGGED)
				usr.gib()
			SSblackbox.record_feedback("nested tally", "arcade_results", 1, list("loss", "mana", (obj_flags & EMAGGED ? "emagged":"normal")))

	else if ((enemy_hp <= 10) && (enemy_mp > 4))
		temp = "[enemy_name] heals for 4 health!"
		playsound(loc, 'sound/arcade/heal.ogg', 50, TRUE, extrarange = -3, falloff = 10)
		enemy_hp += 4
		enemy_mp -= 4

	else
		var/attackamt = rand(3,6)
		temp = "[enemy_name] attacks for [attackamt] damage!"
		playsound(loc, 'sound/arcade/hit.ogg', 50, TRUE, extrarange = -3, falloff = 10)
		player_hp -= attackamt

	if ((player_mp <= 0) || (player_hp <= 0))
		gameover = TRUE
		temp = "You have been crushed! GAME OVER"
		playsound(loc, 'sound/arcade/lose.ogg', 50, TRUE, extrarange = -3, falloff = 10)
		if(obj_flags & EMAGGED)
			usr.gib()
		SSblackbox.record_feedback("nested tally", "arcade_results", 1, list("loss", "hp", (obj_flags & EMAGGED ? "emagged":"normal")))

	blocked = FALSE
	return


/obj/machinery/computer/arcade/battle/emag_act(mob/user)
	if(obj_flags & EMAGGED)
		return
	to_chat(user, "<span class='warning'>A mesmerizing Rhumba beat starts playing from the arcade machine's speakers!</span>")
	temp = "If you die in the game, you die for real!"
	player_hp = 30
	player_mp = 10
	enemy_hp = 45
	enemy_mp = 20
	gameover = FALSE
	blocked = FALSE

	obj_flags |= EMAGGED

	enemy_name = "Cuban Pete"
	name = "Outbomb Cuban Pete"


	updateUsrDialog()



// *** THE ORION TRAIL ** //

/obj/item/gamer_pamphlet
	name = "pamphlet - \'Violent Video Games and You\'"
	desc = "A pamphlet encouraging the reader to maintain a balanced lifestyle and take care of their mental health, while still enjoying video games in a healthy way. You probably don't need this..."
	icon = 'icons/obj/bureaucracy.dmi'
	icon_state = "pamphlet"
	item_state = "paper"
	w_class = WEIGHT_CLASS_TINY


#define ORION_TRAIL_WINTURN		9

//Orion Trail Events
#define ORION_TRAIL_RAIDERS		"Raiders"
#define ORION_TRAIL_FLUX		"Interstellar Flux"
#define ORION_TRAIL_ILLNESS		"Illness"
#define ORION_TRAIL_BREAKDOWN	"Breakdown"
#define ORION_TRAIL_LING		"Changelings?"
#define ORION_TRAIL_LING_ATTACK "Changeling Ambush"
#define ORION_TRAIL_MALFUNCTION	"Malfunction"
#define ORION_TRAIL_COLLISION	"Collision"
#define ORION_TRAIL_SPACEPORT	"Spaceport"
#define ORION_TRAIL_BLACKHOLE	"BlackHole"
#define ORION_TRAIL_OLDSHIP		"Old Ship"
#define ORION_TRAIL_SEARCH		"Old Ship Search"

#define ORION_STATUS_START		1
#define ORION_STATUS_NORMAL		2
#define ORION_STATUS_GAMEOVER	3
#define ORION_STATUS_MARKET		4

/obj/machinery/computer/arcade/orion_trail
	name = "The Orion Trail"
	desc = "Learn how our ancestors got to Orion, and have fun in the process!"
	icon_state = "arcade"
	circuit = /obj/item/circuitboard/computer/arcade/orion_trail
	var/busy = FALSE //prevent clickspam that allowed people to ~speedrun~ the game.
	var/engine = 0
	var/hull = 0
	var/electronics = 0
	var/food = 80
	var/fuel = 60
	var/turns = 4
	var/alive = 4
	var/eventdat = null
	var/event = null
	var/list/settlers = list("Harry","Larry","Bob")
	var/list/events = list(ORION_TRAIL_RAIDERS		= 3,
						   ORION_TRAIL_FLUX			= 1,
						   ORION_TRAIL_ILLNESS		= 3,
						   ORION_TRAIL_BREAKDOWN	= 2,
						   ORION_TRAIL_LING			= 3,
						   ORION_TRAIL_MALFUNCTION	= 2,
						   ORION_TRAIL_COLLISION	= 1,
						   ORION_TRAIL_SPACEPORT	= 2,
						   ORION_TRAIL_OLDSHIP		= 2
						   )
	var/list/stops = list()
	var/list/stopblurbs = list()
	var/lings_aboard = 0
	var/spaceport_raided = 0
	var/spaceport_freebie = 0
	var/last_spaceport_action = ""
	var/gameStatus = ORION_STATUS_START
	var/canContinueEvent = 0

	var/obj/item/radio/Radio
	var/list/gamers = list()
	var/killed_crew = 0


/obj/machinery/computer/arcade/orion_trail/Initialize()
	. = ..()
	Radio = new /obj/item/radio(src)
	Radio.listening = 0

/obj/machinery/computer/arcade/orion_trail/Destroy()
	QDEL_NULL(Radio)
	return ..()

/obj/machinery/computer/arcade/orion_trail/kobayashi
	name = "Kobayashi Maru control computer"
	desc = "A test for cadets"
	icon = 'icons/obj/machines/particle_accelerator.dmi'
	icon_state = "control_boxp"
	events = list("Raiders" = 3, "Interstellar Flux" = 1, "Illness" = 3, "Breakdown" = 2, "Malfunction" = 2, "Collision" = 1, "Spaceport" = 2)
	prize_override = list(/obj/item/paper/fluff/holodeck/trek_diploma = 1)
	settlers = list("Kirk","Worf","Gene")

/obj/machinery/computer/arcade/orion_trail/Reset()
	// Sets up the main trail
	stops = list("Pluto","Asteroid Belt","Proxima Centauri","Dead Space","Rigel Prime","Tau Ceti Beta","Black Hole","Space Outpost Beta-9","Orion Prime")
	stopblurbs = list(
		"Pluto, long since occupied with long-range sensors and scanners, stands ready to, and indeed continues to probe the far reaches of the galaxy.",
		"At the edge of the Sol system lies a treacherous asteroid belt. Many have been crushed by stray asteroids and misguided judgement.",
		"The nearest star system to Sol, in ages past it stood as a reminder of the boundaries of sub-light travel, now a low-population sanctuary for adventurers and traders.",
		"This region of space is particularly devoid of matter. Such low-density pockets are known to exist, but the vastness of it is astounding.",
		"Rigel Prime, the center of the Rigel system, burns hot, basking its planetary bodies in warmth and radiation.",
		"Tau Ceti Beta has recently become a waypoint for colonists headed towards Orion. There are many ships and makeshift stations in the vicinity.",
		"Sensors indicate that a black hole's gravitational field is affecting the region of space we were headed through. We could stay of course, but risk of being overcome by its gravity, or we could change course to go around, which will take longer.",
		"You have come into range of the first man-made structure in this region of space. It has been constructed not by travellers from Sol, but by colonists from Orion. It stands as a monument to the colonists' success.",
		"You have made it to Orion! Congratulations! Your crew is one of the few to start a new foothold for mankind!"
		)

/obj/machinery/computer/arcade/orion_trail/proc/newgame()
	// Set names of settlers in crew
	settlers = list()
	for(var/i = 1; i <= 3; i++)
		add_crewmember()
	add_crewmember("[usr]")
	// Re-set items to defaults
	engine = 1
	hull = 1
	electronics = 1
	food = 80
	fuel = 60
	alive = 4
	turns = 1
	event = null
	gameStatus = ORION_STATUS_NORMAL
	lings_aboard = 0
	killed_crew = 0

	//spaceport junk
	spaceport_raided = 0
	spaceport_freebie = 0
	last_spaceport_action = ""

/obj/machinery/computer/arcade/orion_trail/proc/report_player(mob/gamer)
	if(gamers[gamer] == -2)
		return // enough harassing them

	if(gamers[gamer] == -1)
		say("WARNING: Continued antisocial behavior detected: Dispensing self-help literature.")
		new /obj/item/gamer_pamphlet(get_turf(src))
		gamers[gamer]--
		return

	if(!(gamer in gamers))
		gamers[gamer] = 0

	gamers[gamer]++ // How many times the player has 'prestiged' (massacred their crew)

	if(gamers[gamer] > 2 && prob(20 * gamers[gamer]))

		Radio.set_frequency(FREQ_SECURITY)
		Radio.talk_into(src, "SECURITY ALERT: Crewmember [gamer] recorded displaying antisocial tendencies in [get_area(src)]. Please watch for violent behavior.", FREQ_SECURITY)

		Radio.set_frequency(FREQ_MEDICAL)
		Radio.talk_into(src, "PSYCH ALERT: Crewmember [gamer] recorded displaying antisocial tendencies in [get_area(src)]. Please schedule psych evaluation.", FREQ_MEDICAL)

		gamers[gamer] = -1

		gamer.client.give_award(/datum/award/achievement/misc/gamer, gamer) // PSYCH REPORT NOTE: patient kept rambling about how they did it for an "achievement", recommend continued holding for observation

		if(!isnull(GLOB.data_core.general))
			for(var/datum/data/record/R in GLOB.data_core.general)
				if(R.fields["name"] == gamer.name)
					R.fields["m_stat"] = "*Unstable*"
					return

/obj/machinery/computer/arcade/orion_trail/ui_interact(mob/user)
	. = ..()
	if(fuel <= 0 || food <=0 || settlers.len == 0)
		gameStatus = ORION_STATUS_GAMEOVER
		event = null
	var/dat = ""
	if(gameStatus == ORION_STATUS_GAMEOVER)
		dat = "<center><h1>Game Over</h1></center>"
		dat += "Like many before you, your crew never made it to Orion, lost to space... <br><b>Forever</b>."
		if(!settlers.len)
			dat += "<br>Your entire crew died, and your ship joins the fleet of ghost-ships littering the galaxy."
		else
			if(food <= 0)
				dat += "<br>You ran out of food and starved."
				if(obj_flags & EMAGGED)
					user.set_nutrition(0) //yeah you pretty hongry
					to_chat(user, "<span class='userdanger'>Your body instantly contracts to that of one who has not eaten in months. Agonizing cramps seize you as you fall to the floor.</span>")
			if(fuel <= 0)
				dat += "<br>You ran out of fuel, and drift, slowly, into a star."
				if(obj_flags & EMAGGED)
					var/mob/living/M = user
					M.adjust_fire_stacks(5)
					M.IgniteMob() //flew into a star, so you're on fire
					to_chat(user, "<span class='userdanger'>You feel an immense wave of heat emanate from the arcade machine. Your skin bursts into flames.</span>")

		if(obj_flags & EMAGGED)
			to_chat(user, "<span class='userdanger'>You're never going to make it to Orion...</span>")
			user.death()
			obj_flags &= ~EMAGGED //removes the emagged status after you lose
			gameStatus = ORION_STATUS_START
			name = "The Orion Trail"
			desc = "Learn how our ancestors got to Orion, and have fun in the process!"

		dat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];menu=1'>May They Rest In Peace</a></P>"
	else if(event)
		dat = eventdat
	else if(gameStatus == ORION_STATUS_NORMAL)
		var/title = stops[turns]
		var/subtext = stopblurbs[turns]
		dat = "<center><h1>[title]</h1></center>"
		dat += "[subtext]"
		dat += "<h3><b>Crew:</b></h3>"
		dat += english_list(settlers)
		dat += "<br><b>Food: </b>[food] | <b>Fuel: </b>[fuel]"
		dat += "<br><b>Engine Parts: </b>[engine] | <b>Hull Panels: </b>[hull] | <b>Electronics: </b>[electronics]"
		if(turns == 7)
			dat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];pastblack=1'>Go Around</a> <a href='byond://?src=[REF(src)];blackhole=1'>Continue</a></P>"
		else
			dat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];continue=1'>Continue</a></P>"
		dat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];killcrew=1'>Kill a Crewmember</a></P>"
		dat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];close=1'>Close</a></P>"
	else
		dat = "<center><h2>The Orion Trail</h2></center>"
		dat += "<br><center><h3>Experience the journey of your ancestors!</h3></center><br><br>"
		dat += "<center><b><a href='byond://?src=[REF(src)];newgame=1'>New Game</a></b></center>"
		dat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];close=1'>Close</a></P>"
	var/datum/browser/popup = new(user, "arcade", "The Orion Trail",400,700)
	popup.set_content(dat)
	popup.set_title_image(user.browse_rsc_icon(icon, icon_state))
	popup.open()
	return

/obj/machinery/computer/arcade/orion_trail/Topic(href, href_list)
	if(..())
		return
	if(href_list["close"])
		usr.unset_machine()
		usr << browse(null, "window=arcade")

	if(busy)
		return
	busy = TRUE

	if (href_list["continue"]) //Continue your travels
		if(gameStatus == ORION_STATUS_NORMAL && !event && turns != 7)
			if(turns >= ORION_TRAIL_WINTURN)
				win(usr)
			else
				food -= (alive+lings_aboard)*2
				fuel -= 5
				if(turns == 2 && prob(30))
					event = ORION_TRAIL_COLLISION
					event()
				else if(prob(75))
					event = pickweight(events)
					if(lings_aboard)
						if(event == ORION_TRAIL_LING || prob(55))
							event = ORION_TRAIL_LING_ATTACK
					event()
				turns += 1
			if(obj_flags & EMAGGED)
				var/mob/living/carbon/M = usr //for some vars
				switch(event)
					if(ORION_TRAIL_RAIDERS)
						if(prob(50))
							to_chat(usr, "<span class='userdanger'>You hear battle shouts. The tramping of boots on cold metal. Screams of agony. The rush of venting air. Are you going insane?</span>")
							M.hallucination += 30
						else
							to_chat(usr, "<span class='userdanger'>Something strikes you from behind! It hurts like hell and feel like a blunt weapon, but nothing is there...</span>")
							M.take_bodypart_damage(30)
							playsound(loc, 'sound/weapons/genhit2.ogg', 100, TRUE)
					if(ORION_TRAIL_ILLNESS)
						var/severity = rand(1,3) //pray to RNGesus. PRAY, PIGS
						if(severity == 1)
							to_chat(M, "<span class='userdanger'>You suddenly feel slightly nauseated.</span>" )
						if(severity == 2)
							to_chat(usr, "<span class='userdanger'>You suddenly feel extremely nauseated and hunch over until it passes.</span>")
							M.Stun(60)
						if(severity >= 3) //you didn't pray hard enough
							to_chat(M, "<span class='warning'>An overpowering wave of nausea consumes over you. You hunch over, your stomach's contents preparing for a spectacular exit.</span>")
							M.Stun(100)
							sleep(30)
							M.vomit(10, distance = 5)
					if(ORION_TRAIL_FLUX)
						if(prob(75))
							M.Paralyze(60)
							say("A sudden gust of powerful wind slams [M] into the floor!")
							M.take_bodypart_damage(25)
							playsound(loc, 'sound/weapons/genhit.ogg', 100, TRUE)
						else
							to_chat(M, "<span class='userdanger'>A violent gale blows past you, and you barely manage to stay standing!</span>")
					if(ORION_TRAIL_COLLISION) //by far the most damaging event
						if(prob(90))
							playsound(loc, 'sound/effects/bang.ogg', 100, TRUE)
							var/turf/open/floor/F
							for(F in orange(1, src))
								F.ScrapeAway()
							say("Something slams into the floor around [src], exposing it to space!")
							if(hull)
								sleep(10)
								say("A new floor suddenly appears around [src]. What the hell?")
								playsound(loc, 'sound/weapons/genhit.ogg', 100, TRUE)
								var/turf/open/space/T
								for(T in orange(1, src))
									T.PlaceOnTop(/turf/open/floor/plating)
						else
							say("Something slams into the floor around [src] - luckily, it didn't get through!")
							playsound(loc, 'sound/effects/bang.ogg', 50, TRUE)
					if(ORION_TRAIL_MALFUNCTION)
						playsound(loc, 'sound/effects/empulse.ogg', 50, TRUE)
						visible_message("<span class='danger'>[src] malfunctions, randomizing in-game stats!</span>")
						var/oldfood = food
						var/oldfuel = fuel
						food = rand(10,80) / rand(1,2)
						fuel = rand(10,60) / rand(1,2)
						if(electronics)
							sleep(10)
							if(oldfuel > fuel && oldfood > food)
								audible_message("<span class='danger'>[src] lets out a somehow reassuring chime.</span>")
							else if(oldfuel < fuel || oldfood < food)
								audible_message("<span class='danger'>[src] lets out a somehow ominous chime.</span>")
							food = oldfood
							fuel = oldfuel
							playsound(loc, 'sound/machines/chime.ogg', 50, TRUE)

	else if(href_list["newgame"]) //Reset everything
		if(gameStatus == ORION_STATUS_START)
			newgame()
	else if(href_list["menu"]) //back to the main menu
		if(gameStatus == ORION_STATUS_GAMEOVER)
			gameStatus = ORION_STATUS_START
			event = null
			food = 80
			fuel = 60
			settlers = list("Harry","Larry","Bob")
	else if(href_list["search"]) //search old ship
		if(event == ORION_TRAIL_OLDSHIP)
			event = ORION_TRAIL_SEARCH
			event()
	else if(href_list["slow"]) //slow down
		if(event == ORION_TRAIL_FLUX)
			food -= (alive+lings_aboard)*2
			fuel -= 5
		event = null
	else if(href_list["pastblack"]) //slow down
		if(turns == 7)
			food -= ((alive+lings_aboard)*2)*3
			fuel -= 15
			turns += 1
			event = null
	else if(href_list["useengine"]) //use parts
		if(event == ORION_TRAIL_BREAKDOWN)
			engine = max(0, --engine)
			event = null
	else if(href_list["useelec"]) //use parts
		if(event == ORION_TRAIL_MALFUNCTION)
			electronics = max(0, --electronics)
			event = null
	else if(href_list["usehull"]) //use parts
		if(event == ORION_TRAIL_COLLISION)
			hull = max(0, --hull)
			event = null
	else if(href_list["wait"]) //wait 3 days
		if(event == ORION_TRAIL_BREAKDOWN || event == ORION_TRAIL_MALFUNCTION || event == ORION_TRAIL_COLLISION)
			food -= ((alive+lings_aboard)*2)*3
			event = null
	else if(href_list["keepspeed"]) //keep speed
		if(event == ORION_TRAIL_FLUX)
			if(prob(75))
				event = "Breakdown"
				event()
			else
				event = null
	else if(href_list["blackhole"]) //keep speed past a black hole
		if(turns == 7)
			if(prob(75))
				event = ORION_TRAIL_BLACKHOLE
				event()
				if(obj_flags & EMAGGED)
					playsound(loc, 'sound/effects/supermatter.ogg', 100, TRUE)
					say("A miniature black hole suddenly appears in front of [src], devouring [usr] alive!")
					if(isliving(usr))
						var/mob/living/L = usr
						L.Stun(200, ignore_canstun = TRUE) //you can't run :^)
					var/S = new /obj/singularity/academy(usr.loc)
					addtimer(CALLBACK(src, /atom/movable/proc/say, "[S] winks out, just as suddenly as it appeared."), 50)
					QDEL_IN(S, 50)
			else
				event = null
				turns += 1
	else if(href_list["holedeath"])
		if(event == ORION_TRAIL_BLACKHOLE)
			gameStatus = ORION_STATUS_GAMEOVER
			event = null
	else if(href_list["eventclose"]) //end an event
		if(canContinueEvent)
			event = null

	else if(href_list["killcrew"]) //shoot a crewmember
		if(gameStatus == ORION_STATUS_NORMAL || event == ORION_TRAIL_LING)
			var/sheriff = remove_crewmember() //I shot the sheriff
			playsound(loc,'sound/weapons/gun/pistol/shot.ogg', 100, TRUE)
			killed_crew++

			if(settlers.len == 0 || alive == 0)
				say("The last crewmember [sheriff], shot themselves, GAME OVER!")
				if(obj_flags & EMAGGED)
					usr.death(0)
					obj_flags &= EMAGGED
				gameStatus = ORION_STATUS_GAMEOVER
				event = null

				if(killed_crew >= 4)
					report_player(usr)
			else if(obj_flags & EMAGGED)
				if(usr.name == sheriff)
					say("The crew of the ship chose to kill [usr.name]!")
					usr.death(0)

			if(event == ORION_TRAIL_LING) //only ends the ORION_TRAIL_LING event, since you can do this action in multiple places
				event = null
				killed_crew-- // the kill was valid

	//Spaceport specific interactions
	//they get a header because most of them don't reset event (because it's a shop, you leave when you want to)
	//they also call event() again, to regen the eventdata, which is kind of odd but necessary
	else if(href_list["buycrew"]) //buy a crewmember
		if(gameStatus == ORION_STATUS_MARKET)
			if(!spaceport_raided && food >= 10 && fuel >= 10)
				var/bought = add_crewmember()
				last_spaceport_action = "You hired [bought] as a new crewmember."
				fuel -= 10
				food -= 10
				event()
				killed_crew-- // I mean not really but you know

	else if(href_list["sellcrew"]) //sell a crewmember
		if(gameStatus == ORION_STATUS_MARKET)
			if(!spaceport_raided && settlers.len > 1)
				var/sold = remove_crewmember()
				last_spaceport_action = "You sold your crewmember, [sold]!"
				fuel += 7
				food += 7
				event()

	else if(href_list["leave_spaceport"])
		if(gameStatus == ORION_STATUS_MARKET)
			event = null
			gameStatus = ORION_STATUS_NORMAL
			spaceport_raided = 0
			spaceport_freebie = 0
			last_spaceport_action = ""

	else if(href_list["raid_spaceport"])
		if(gameStatus == ORION_STATUS_MARKET)
			if(!spaceport_raided)
				var/success = min(15 * alive,100) //default crew (4) have a 60% chance
				spaceport_raided = 1

				var/FU = 0
				var/FO = 0
				if(prob(success))
					FU = rand(5,15)
					FO = rand(5,15)
					last_spaceport_action = "You successfully raided the spaceport! You gained [FU] Fuel and [FO] Food! (+[FU]FU,+[FO]FO)"
				else
					FU = rand(-5,-15)
					FO = rand(-5,-15)
					last_spaceport_action = "You failed to raid the spaceport! You lost [FU*-1] Fuel and [FO*-1] Food in your scramble to escape! ([FU]FU,[FO]FO)"

					//your chance of lose a crewmember is 1/2 your chance of success
					//this makes higher % failures hurt more, don't get cocky space cowboy!
					if(prob(success*5))
						var/lost_crew = remove_crewmember()
						last_spaceport_action = "You failed to raid the spaceport! You lost [FU*-1] Fuel and [FO*-1] Food, AND [lost_crew] in your scramble to escape! ([FU]FI,[FO]FO,-Crew)"
						if(obj_flags & EMAGGED)
							say("WEEWOO! WEEWOO! Spaceport security en route!")
							playsound(src, 'sound/items/weeoo1.ogg', 100, FALSE)
							for(var/i, i<=3, i++)
								var/mob/living/simple_animal/hostile/syndicate/ranged/smg/orion/O = new/mob/living/simple_animal/hostile/syndicate/ranged/smg/orion(get_turf(src))
								O.target = usr


				fuel += FU
				food += FO
				event()

	else if(href_list["buyparts"])
		if(gameStatus == ORION_STATUS_MARKET)
			if(!spaceport_raided && fuel > 5)
				switch(text2num(href_list["buyparts"]))
					if(1) //Engine Parts
						engine++
						last_spaceport_action = "Bought Engine Parts"
					if(2) //Hull Plates
						hull++
						last_spaceport_action = "Bought Hull Plates"
					if(3) //Spare Electronics
						electronics++
						last_spaceport_action = "Bought Spare Electronics"
				fuel -= 5 //they all cost 5
				event()

	else if(href_list["trade"])
		if(gameStatus == ORION_STATUS_MARKET)
			if(!spaceport_raided)
				switch(text2num(href_list["trade"]))
					if(1) //Fuel
						if(fuel > 5)
							fuel -= 5
							food += 5
							last_spaceport_action = "Traded Fuel for Food"
							event()
					if(2) //Food
						if(food > 5)
							fuel += 5
							food -= 5
							last_spaceport_action = "Traded Food for Fuel"
							event()

	add_fingerprint(usr)
	updateUsrDialog()
	busy = FALSE
	return


/obj/machinery/computer/arcade/orion_trail/proc/event()
	eventdat = "<center><h1>[event]</h1></center>"
	canContinueEvent = 0
	switch(event)
		if(ORION_TRAIL_RAIDERS)
			eventdat += "Raiders have come aboard your ship!"
			if(prob(50))
				var/sfood = rand(1,10)
				var/sfuel = rand(1,10)
				food -= sfood
				fuel -= sfuel
				eventdat += "<br>They have stolen [sfood] <b>Food</b> and [sfuel] <b>Fuel</b>."
			else if(prob(10))
				var/deadname = remove_crewmember()
				eventdat += "<br>[deadname] tried to fight back, but was killed."
			else
				eventdat += "<br>Fortunately, you fended them off without any trouble."
			eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];eventclose=1'>Continue</a></P>"
			eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];close=1'>Close</a></P>"
			canContinueEvent = 1

		if(ORION_TRAIL_FLUX)
			eventdat += "This region of space is highly turbulent. <br>If we go slowly we may avoid more damage, but if we keep our speed we won't waste supplies."
			eventdat += "<br>What will you do?"
			eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];slow=1'>Slow Down</a> <a href='byond://?src=[REF(src)];keepspeed=1'>Continue</a></P>"
			eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];close=1'>Close</a></P>"

		if(ORION_TRAIL_OLDSHIP)
			eventdat += "<br>Your crew spots an old ship floating through space. It might have some supplies, but then again it looks rather unsafe."
			eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];search=1'>Search it</a><a href='byond://?src=[REF(src)];eventclose=1'>Leave it</a></P><P ALIGN=Right><a href='byond://?src=[REF(src)];close=1'>Close</a></P>"
			canContinueEvent = 1

		if(ORION_TRAIL_SEARCH)
			switch(rand(100))
				if(0 to 15)
					var/rescued = add_crewmember()
					var/oldfood = rand(1,7)
					var/oldfuel = rand(4,10)
					food += oldfood
					fuel += oldfuel
					eventdat += "<br>As you look through it you find some supplies and a living person!"
					eventdat += "<br>[rescued] was rescued from the abandoned ship!"
					eventdat += "<br>You found [oldfood] <b>Food</b> and [oldfuel] <b>Fuel</b>."
				if(15 to 35)
					var/lfuel = rand(4,7)
					var/deadname = remove_crewmember()
					fuel -= lfuel
					eventdat += "<br>[deadname] was lost deep in the wreckage, and your own vessel lost [lfuel] <b>Fuel</b> maneuvering to the the abandoned ship."
				if(35 to 65)
					var/oldfood = rand(5,11)
					food += oldfood
					engine++
					eventdat += "<br>You found [oldfood] <b>Food</b> and some parts amongst the wreck."
				else
					eventdat += "<br>As you look through the wreck you cannot find much of use."
			eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];eventclose=1'>Continue</a></P>"
			eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];close=1'>Close</a></P>"
			canContinueEvent = 1

		if(ORION_TRAIL_ILLNESS)
			eventdat += "A deadly illness has been contracted!"
			var/deadname = remove_crewmember()
			eventdat += "<br>[deadname] was killed by the disease."
			eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];eventclose=1'>Continue</a></P>"
			eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];close=1'>Close</a></P>"
			canContinueEvent = 1

		if(ORION_TRAIL_BREAKDOWN)
			eventdat += "Oh no! The engine has broken down!"
			eventdat += "<br>You can repair it with an engine part, or you can make repairs for 3 days."
			if(engine >= 1)
				eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];useengine=1'>Use Part</a><a href='byond://?src=[REF(src)];wait=1'>Wait</a></P>"
			else
				eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];wait=1'>Wait</a></P>"
			eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];close=1'>Close</a></P>"

		if(ORION_TRAIL_MALFUNCTION)
			eventdat += "The ship's systems are malfunctioning!"
			eventdat += "<br>You can replace the broken electronics with spares, or you can spend 3 days troubleshooting the AI."
			if(electronics >= 1)
				eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];useelec=1'>Use Part</a><a href='byond://?src=[REF(src)];wait=1'>Wait</a></P>"
			else
				eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];wait=1'>Wait</a></P>"
			eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];close=1'>Close</a></P>"

		if(ORION_TRAIL_COLLISION)
			eventdat += "Something hit us! Looks like there's some hull damage."
			if(prob(25))
				var/sfood = rand(5,15)
				var/sfuel = rand(5,15)
				food -= sfood
				fuel -= sfuel
				eventdat += "<br>[sfood] <b>Food</b> and [sfuel] <b>Fuel</b> was vented out into space."
			if(prob(10))
				var/deadname = remove_crewmember()
				eventdat += "<br>[deadname] was killed by rapid depressurization."
			eventdat += "<br>You can repair the damage with hull plates, or you can spend the next 3 days welding scrap together."
			if(hull >= 1)
				eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];usehull=1'>Use Part</a><a href='byond://?src=[REF(src)];wait=1'>Wait</a></P>"
			else
				eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];wait=1'>Wait</a></P>"
			eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];close=1'>Close</a></P>"

		if(ORION_TRAIL_BLACKHOLE)
			eventdat += "You were swept away into the black hole."
			eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];holedeath=1'>Oh...</a></P>"
			eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];close=1'>Close</a></P>"
			settlers = list()

		if(ORION_TRAIL_LING)
			eventdat += "Strange reports warn of changelings infiltrating crews on trips to Orion..."
			if(settlers.len <= 2)
				eventdat += "<br>Your crew's chance of reaching Orion is so slim the changelings likely avoided your ship..."
				eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];eventclose=1'>Continue</a></P>"
				eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];close=1'>Close</a></P>"
				if(prob(10)) // "likely", I didn't say it was guaranteed!
					lings_aboard = min(++lings_aboard,2)
			else
				if(lings_aboard) //less likely to stack lings
					if(prob(20))
						lings_aboard = min(++lings_aboard,2)
				else if(prob(70))
					lings_aboard = min(++lings_aboard,2)

				eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];killcrew=1'>Kill a Crewmember</a></P>"
				eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];eventclose=1'>Risk it</a></P>"
				eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];close=1'>Close</a></P>"
			canContinueEvent = 1

		if(ORION_TRAIL_LING_ATTACK)
			if(lings_aboard <= 0) //shouldn't trigger, but hey.
				eventdat += "Haha, fooled you, there are no changelings on board!"
				eventdat += "<br>(You should report this to a coder :S)"
			else
				var/ling1 = remove_crewmember()
				var/ling2 = ""
				if(lings_aboard >= 2)
					ling2 = remove_crewmember()

				eventdat += "Changelings among your crew suddenly burst from hiding and attack!"
				if(ling2)
					eventdat += "<br>[ling1] and [ling2]'s arms twist and contort into grotesque blades!"
				else
					eventdat += "<br>[ling1]'s arm twists and contorts into a grotesque blade!"

				var/chance2attack = alive*20
				if(prob(chance2attack))
					var/chancetokill = 30*lings_aboard-(5*alive) //eg: 30*2-(10) = 50%, 2 lings, 2 crew is 50% chance
					if(prob(chancetokill))
						var/deadguy = remove_crewmember()
						var/murder_text = pick("The changeling[ling2 ? "s" : ""] bring[ling2 ? "" : "s"] down [deadguy] and disembowel[ling2 ? "" : "s"] them in a spray of gore!", \
						"[ling2 ? pick(ling1, ling2) : ling1] corners [deadguy] and impales them through the stomach!", \
						"[ling2 ? pick(ling1, ling2) : ling1] decapitates [deadguy] in a single cleaving arc!")
						eventdat += "<br>[murder_text]"
					else
						eventdat += "<br><br><b>You valiantly fight off the changeling[ling2 ? "s":""]!</b>"
						if(ling2)
							food += 30
							lings_aboard = max(0,lings_aboard-2)
						else
							food += 15
							lings_aboard = max(0,--lings_aboard)
						eventdat += "<br><i>Well, it's perfectly good food...</i>\
						<br>You cut the changeling[ling2 ? "s" : ""] into meat, gaining <b>[ling2 ? "30" : "15"]</b> Food!"
				else
					eventdat += "<br><br>[pick("Sensing unfavorable odds", "After a failed attack", "Suddenly breaking nerve")], \
					the changeling[ling2 ? "s":""] vanish[ling2 ? "" : "es"] into space through the airlocks! You're safe... for now."
					if(ling2)
						lings_aboard = max(0,lings_aboard-2)
					else
						lings_aboard = max(0,--lings_aboard)

			eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];eventclose=1'>Continue</a></P>"
			eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];close=1'>Close</a></P>"
			canContinueEvent = 1


		if(ORION_TRAIL_SPACEPORT)
			gameStatus = ORION_STATUS_MARKET
			if(spaceport_raided)
				eventdat += "The spaceport is on high alert! You've been barred from docking by the local authorities after your failed raid."
				if(last_spaceport_action)
					eventdat += "<br><b>Last Spaceport Action:</b> [last_spaceport_action]"
				eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];leave_spaceport=1'>Depart Spaceport</a></P>"
				eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];close=1'>Close</a></P>"
			else
				eventdat += "Your jump into the sector yields a spaceport - a lucky find!"
				eventdat += "<br>This spaceport is home to travellers who failed to reach Orion, but managed to find a different home..."
				eventdat += "<br>Trading terms: FU = Fuel, FO = Food"
				if(last_spaceport_action)
					eventdat += "<br><b>Last action:</b> [last_spaceport_action]"
				eventdat += "<h3><b>Crew:</b></h3>"
				eventdat += english_list(settlers)
				eventdat += "<br><b>Food: </b>[food] | <b>Fuel: </b>[fuel]"
				eventdat += "<br><b>Engine Parts: </b>[engine] | <b>Hull Panels: </b>[hull] | <b>Electronics: </b>[electronics]"


				//If your crew is pathetic you can get freebies (provided you haven't already gotten one from this port)
				if(!spaceport_freebie && (fuel < 20 || food < 20))
					spaceport_freebie++
					var/FU = 10
					var/FO = 10
					var/freecrew = 0
					if(prob(30))
						FU = 25
						FO = 25

					if(prob(10))
						add_crewmember()
						freecrew++

					eventdat += "<br>The traders of the spaceport take pity on you, and generously give you some free supplies! (+[FU]FU, +[FO]FO)"
					if(freecrew)
						eventdat += "<br>You also gain a new crewmember!"

					fuel += FU
					food += FO

				//CREW INTERACTIONS
				eventdat += "<P ALIGN=Right>Crew Management:</P>"

				//Buy crew
				if(food >= 10 && fuel >= 10)
					eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];buycrew=1'>Hire a New Crewmember (-10FU, -10FO)</a></P>"
				else
					eventdat += "<P ALIGN=Right>You cannot afford a new crewmember.</P>"

				//Sell crew
				if(settlers.len > 1)
					eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];sellcrew=1'>Sell Crew for Fuel and Food (+7FU, +7FO)</a></P>"
				else
					eventdat += "<P ALIGN=Right>You have no other crew to sell.</P>"

				//BUY/SELL STUFF
				eventdat += "<P ALIGN=Right>Spare Parts:</P>"

				//Engine parts
				if(fuel > 5)
					eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];buyparts=1'>Buy Engine Parts (-5FU)</a></P>"
				else
					eventdat += "<P ALIGN=Right>You cannot afford engine parts.</a>"

				//Hull plates
				if(fuel > 5)
					eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];buyparts=2'>Buy Hull Plates (-5FU)</a></P>"
				else
					eventdat += "<P ALIGN=Right>You cannot afford hull plates.</a>"

				//Electronics
				if(fuel > 5)
					eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];buyparts=3'>Buy Spare Electronics (-5FU)</a></P>"
				else
					eventdat += "<P ALIGN=Right>You cannot afford spare electronics.</a>"

				//Trade
				if(fuel > 5)
					eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];trade=1'>Trade Fuel for Food (-5FU,+5FO)</a></P>"
				else
					eventdat += "<P ALIGN=Right>You don't have 5FU to trade.</P"

				if(food > 5)
					eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];trade=2'>Trade Food for Fuel (+5FU,-5FO)</a></P>"
				else
					eventdat += "<P ALIGN=Right>You don't have 5FO to trade.</P"

				//Raid the spaceport
				eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];raid_spaceport=1'>!! Raid Spaceport !!</a></P>"

				eventdat += "<P ALIGN=Right><a href='byond://?src=[REF(src)];leave_spaceport=1'>Depart Spaceport</a></P>"


//Add Random/Specific crewmember
/obj/machinery/computer/arcade/orion_trail/proc/add_crewmember(var/specific = "")
	var/newcrew = ""
	if(specific)
		newcrew = specific
	else
		if(prob(50))
			newcrew = pick(GLOB.first_names_male)
		else
			newcrew = pick(GLOB.first_names_female)
	if(newcrew)
		settlers += newcrew
		alive++
	return newcrew


//Remove Random/Specific crewmember
/obj/machinery/computer/arcade/orion_trail/proc/remove_crewmember(specific = "", dont_remove = "")
	var/list/safe2remove = settlers
	var/removed = ""
	if(dont_remove)
		safe2remove -= dont_remove
	if(specific && specific != dont_remove)
		safe2remove = list(specific)
	else
		removed = pick(safe2remove)

	if(removed)
		if(lings_aboard && prob(40*lings_aboard)) //if there are 2 lings you're twice as likely to get one, obviously
			lings_aboard = max(0,--lings_aboard)
		settlers -= removed
		alive--
	return removed


/obj/machinery/computer/arcade/orion_trail/proc/win(mob/user)
	gameStatus = ORION_STATUS_START
	say("Congratulations, you made it to Orion!")
	if(obj_flags & EMAGGED)
		new /obj/item/orion_ship(loc)
		message_admins("[ADMIN_LOOKUPFLW(usr)] made it to Orion on an emagged machine and got an explosive toy ship.")
		log_game("[key_name(usr)] made it to Orion on an emagged machine and got an explosive toy ship.")
	else
		prizevend(user)
	obj_flags &= ~EMAGGED
	name = "The Orion Trail"
	desc = "Learn how our ancestors got to Orion, and have fun in the process!"

/obj/machinery/computer/arcade/orion_trail/emag_act(mob/user)
	if(obj_flags & EMAGGED)
		return
	to_chat(user, "<span class='notice'>You override the cheat code menu and skip to Cheat #[rand(1, 50)]: Realism Mode.</span>")
	name = "The Orion Trail: Realism Edition"
	desc = "Learn how our ancestors got to Orion, and try not to die in the process!"
	newgame()
	obj_flags |= EMAGGED

/mob/living/simple_animal/hostile/syndicate/ranged/smg/orion
	name = "spaceport security"
	desc = "Premier corporate security forces for all spaceports found along the Orion Trail."
	faction = list("orion")
	loot = list()
	del_on_death = TRUE

/obj/item/orion_ship
	name = "model settler ship"
	desc = "A model spaceship, it looks like those used back in the day when travelling to Orion! It even has a miniature FX-293 reactor, which was renowned for its instability and tendency to explode..."
	icon = 'icons/obj/toy.dmi'
	icon_state = "ship"
	w_class = WEIGHT_CLASS_SMALL
	var/active = 0 //if the ship is on

/obj/item/orion_ship/examine(mob/user)
	. = ..()
	if(!(in_range(user, src)))
		return
	if(!active)
		. += "<span class='notice'>There's a little switch on the bottom. It's flipped down.</span>"
	else
		. += "<span class='notice'>There's a little switch on the bottom. It's flipped up.</span>"

/obj/item/orion_ship/attack_self(mob/user) //Minibomb-level explosion. Should probably be more because of how hard it is to survive the machine! Also, just over a 5-second fuse
	if(active)
		return

	log_bomber(usr, "primed an explosive", src, "for detonation")

	to_chat(user, "<span class='warning'>You flip the switch on the underside of [src].</span>")
	active = 1
	visible_message("<span class='notice'>[src] softly beeps and whirs to life!</span>")
	playsound(loc, 'sound/machines/defib_SaftyOn.ogg', 25, TRUE)
	say("This is ship ID #[rand(1,1000)] to Orion Port Authority. We're coming in for landing, over.")
	sleep(20)
	visible_message("<span class='warning'>[src] begins to vibrate...</span>")
	say("Uh, Port? Having some issues with our reactor, could you check it out? Over.")
	sleep(30)
	say("Oh, God! Code Eight! CODE EIGHT! IT'S GONNA BL-")
	playsound(loc, 'sound/machines/buzz-sigh.ogg', 25, TRUE)
	sleep(3.6)
	visible_message("<span class='userdanger'>[src] explodes!</span>")
	explosion(loc, 2,4,8, flame_range = 16)
	qdel(src)

// ** AMPUTATION ** //

/obj/machinery/computer/arcade/amputation
	name = "Mediborg's Amputation Adventure"
	desc = "A picture of a blood-soaked medical cyborg flashes on the screen. The mediborg has a speech bubble that says, \"Put your hand in the machine if you aren't a <b>coward!</b>\""
	icon_state = "arcade"
	circuit = /obj/item/circuitboard/computer/arcade/amputation

/obj/machinery/computer/arcade/amputation/attack_hand(mob/user)
	if(!iscarbon(user))
		return
	var/mob/living/carbon/c_user = user
	if(!c_user.get_bodypart(BODY_ZONE_L_ARM) && !c_user.get_bodypart(BODY_ZONE_R_ARM))
		return
	to_chat(c_user, "<span class='warning'>You move your hand towards the machine, and begin to hesitate as a bloodied guillotine emerges from inside of it...</span>")
	if(do_after(c_user, 50, target = src))
		to_chat(c_user, "<span class='userdanger'>The guillotine drops on your arm, and the machine sucks it in!</span>")
		playsound(loc, 'sound/weapons/slice.ogg', 25, TRUE, -1)
		var/which_hand = BODY_ZONE_L_ARM
		if(!(c_user.active_hand_index % 2))
			which_hand = BODY_ZONE_R_ARM
		var/obj/item/bodypart/chopchop = c_user.get_bodypart(which_hand)
		chopchop.dismember()
		qdel(chopchop)
		playsound(loc, 'sound/arcade/win.ogg', 50, TRUE, extrarange = -3, falloff = 10)
		for(var/i=1; i<=rand(3,5); i++)
			prizevend(user)
	else
		to_chat(c_user, "<span class='notice'>You (wisely) decide against putting your hand in the machine.</span>")

/obj/machinery/computer/arcade/amputation/festive //dispenses wrapped gifts instead of arcade prizes, also known as the ancap christmas tree
	name = "Mediborg's Festive Amputation Adventure"
	desc = "A picture of a blood-soaked medical cyborg wearing a Santa hat flashes on the screen. The mediborg has a speech bubble that says, \"Put your hand in the machine if you aren't a <b>coward!</b>\""
	prize_override = list(/obj/item/a_gift/anything = 1)

#undef ORION_TRAIL_WINTURN
#undef ORION_TRAIL_RAIDERS
#undef ORION_TRAIL_FLUX
#undef ORION_TRAIL_ILLNESS
#undef ORION_TRAIL_BREAKDOWN
#undef ORION_TRAIL_LING
#undef ORION_TRAIL_LING_ATTACK
#undef ORION_TRAIL_MALFUNCTION
#undef ORION_TRAIL_COLLISION
#undef ORION_TRAIL_SPACEPORT
#undef ORION_TRAIL_BLACKHOLE
#undef ORION_TRAIL_OLDSHIP
#undef ORION_TRAIL_SEARCH

#undef ORION_STATUS_START
#undef ORION_STATUS_NORMAL
#undef ORION_STATUS_GAMEOVER
#undef ORION_STATUS_MARKET
