//Snow Valley Areas//--

/area/awaymission/snowdin
	name = "Snowdin"
	icon_state = "awaycontent1"
	requires_power = FALSE
	dynamic_lighting = DYNAMIC_LIGHTING_DISABLED

/area/awaymission/snowdin/outside
	name = "Snowdin Tundra Plains"
	icon_state = "awaycontent25"

/area/awaymission/snowdin/post
	name = "Snowdin Outpost"
	icon_state = "awaycontent2"
	requires_power = TRUE
	dynamic_lighting = DYNAMIC_LIGHTING_ENABLED

/area/awaymission/snowdin/post/medbay
	name = "Snowdin Outpost - Medbay"
	icon_state = "awaycontent3"

/area/awaymission/snowdin/post/secpost
	name = "Snowdin Outpost - Security Checkpoint"
	icon_state = "awaycontent4"

/area/awaymission/snowdin/post/hydro
	name = "Snowdin Outpost - Hydroponics"
	icon_state = "awaycontent5"

/area/awaymission/snowdin/post/messhall
	name = "Snowdin Outpost - Mess Hall"
	icon_state = "awaycontent6"

/area/awaymission/snowdin/post/gateway
	name = "Snowdin Outpost - Gateway"
	icon_state = "awaycontent7"

/area/awaymission/snowdin/post/dorm
	name = "Snowdin Outpost - Dorms"
	icon_state = "awaycontent8"

/area/awaymission/snowdin/post/kitchen
	name = "Snowdin Outpost - Kitchen"
	icon_state = "awaycontent9"

/area/awaymission/snowdin/post/engineering
	name = "Snowdin Outpost - Engineering"
	icon_state = "awaycontent10"

/area/awaymission/snowdin/post/custodials
	name = "Snowdin Outpost - Custodials"
	icon_state = "awaycontent11"

/area/awaymission/snowdin/post/research
	name = "Snowdin Outpost - Research Area"
	icon_state = "awaycontent12"

/area/awaymission/snowdin/post/garage
	name = "Snowdin Outpost - Garage"
	icon_state = "awaycontent13"

/area/awaymission/snowdin/post/minipost
	name = "Snowdin Outpost - Recon Post"
	icon_state = "awaycontent19"

/area/awaymission/snowdin/post/mining_main
	name = "Snowdin Outpost - Mining Post"
	icon_state = "awaycontent21"

/area/awaymission/snowdin/post/mining_main/mechbay
	name = "Snowdin Outpost - Mining Post Mechbay"
	icon_state = "awaycontent25"

/area/awaymission/snowdin/post/mining_main/robotics
	name = "Snowdin Outpost - Mining Post Robotics"
	icon_state = "awaycontent26"

/area/awaymission/snowdin/post/cavern1
	name = "Snowdin Outpost - Cavern Outpost 1"
	icon_state = "awaycontent27"

/area/awaymission/snowdin/post/cavern2
	name = "Snowdin Outpost - Cavern Outpost 2"
	icon_state = "awaycontent28"

/area/awaymission/snowdin/post/mining_dock
	name = "Snowdin Outpost - Underground Mine Post"
	icon_state = "awaycontent22"

/area/awaymission/snowdin/post/broken_shuttle
	name = "Snowdin Outpost - Broken Transit Shuttle"
	icon_state = "awaycontent20"
	requires_power = FALSE

/area/awaymission/snowdin/igloo
	name = "Snowdin Igloos"
	icon_state = "awaycontent14"
	dynamic_lighting = DYNAMIC_LIGHTING_FORCED

/area/awaymission/snowdin/cave
	name = "Snowdin Caves"
	icon_state = "awaycontent15"
	dynamic_lighting = DYNAMIC_LIGHTING_FORCED

/area/awaymission/snowdin/cave/cavern
	name = "Snowdin Depths"
	icon_state = "awaycontent23"

/area/awaymission/snowdin/cave/mountain
	name = "Snowdin Mountains"
	icon_state = "awaycontent24"


/area/awaymission/snowdin/base
	name = "Snowdin Main Base"
	icon_state = "awaycontent16"
	dynamic_lighting = DYNAMIC_LIGHTING_ENABLED
	requires_power = TRUE

/area/awaymission/snowdin/dungeon1
	name = "Snowdin Depths"
	icon_state = "awaycontent17"
	dynamic_lighting = DYNAMIC_LIGHTING_ENABLED

/area/awaymission/snowdin/sekret
	name = "Snowdin Operations"
	icon_state = "awaycontent18"
	dynamic_lighting = DYNAMIC_LIGHTING_ENABLED
	requires_power = TRUE

/area/shuttle/snowdin/elevator1
	name = "Excavation Elevator"

/area/shuttle/snowdin/elevator2
	name = "Mining Elevator"

//shuttle console for elevators//

/obj/machinery/computer/shuttle/snowdin/mining
	name = "shuttle console"
	desc = "A shuttle control computer."
	icon_screen = "shuttle"
	icon_keyboard = "tech_key"
	light_color = LIGHT_COLOR_CYAN
	shuttleId = "snowdin_mining"
	possible_destinations = "snowdin_mining_top;snowdin_mining_down"


//liquid plasma!!!!!!//

/turf/open/floor/plasteel/dark/snowdin
	initial_gas_mix = FROZEN_ATMOS
	planetary_atmos = 1
	temperature = 180

/turf/open/lava/plasma
	name = "liquid plasma"
	desc = "A flowing stream of chilled liquid plasma. You probably shouldn't get in."
	icon_state = "liquidplasma"
	initial_gas_mix = "o2=0;n2=82;plasma=24;TEMP=120"
	baseturfs = /turf/open/lava/plasma
	slowdown = 2

	light_range = 3
	light_power = 0.75
	light_color = LIGHT_COLOR_PURPLE

/turf/open/lava/plasma/attackby(obj/item/I, mob/user, params)
	var/obj/item/reagent_containers/glass/C = I
	if(C.reagents.total_volume >= C.volume)
		to_chat(user, "<span class='danger'>[C] is full.</span>")
		return
	C.reagents.add_reagent(/datum/reagent/toxin/plasma, rand(5, 10))
	user.visible_message("<span class='notice'>[user] scoops some plasma from the [src] with \the [C].</span>", "<span class='notice'>You scoop out some plasma from the [src] using \the [C].</span>")

/turf/open/lava/plasma/burn_stuff(AM)
	. = 0

	if(is_safe())
		return FALSE

	var/thing_to_check = src
	if (AM)
		thing_to_check = list(AM)
	for(var/thing in thing_to_check)
		if(isobj(thing))
			var/obj/O = thing
			if((O.resistance_flags & (FREEZE_PROOF)) || O.throwing)
				continue

		else if (isliving(thing))
			. = 1
			var/mob/living/L = thing
			if(L.movement_type & FLYING)
				continue	//YOU'RE FLYING OVER IT
			if("snow" in L.weather_immunities)
				continue

			var/buckle_check = L.buckling
			if(!buckle_check)
				buckle_check = L.buckled
			if(isobj(buckle_check))
				var/obj/O = buckle_check
				if(O.resistance_flags & FREEZE_PROOF)
					continue

			else if(isliving(buckle_check))
				var/mob/living/live = buckle_check
				if("snow" in live.weather_immunities)
					continue

			L.adjustFireLoss(2)
			if(L)
				L.adjust_fire_stacks(20) //dipping into a stream of plasma would probably make you more flammable than usual
				L.adjust_bodytemperature(-rand(50,65)) //its cold, man
				if(ishuman(L))//are they a carbon?
					var/list/plasma_parts = list()//a list of the organic parts to be turned into plasma limbs
					var/list/robo_parts = list()//keep a reference of robotic parts so we know if we can turn them into a plasmaman
					var/mob/living/carbon/human/PP = L
					var/S = PP.dna.species
					if(istype(S, /datum/species/plasmaman) || istype(S, /datum/species/android) || istype(S, /datum/species/synth)) //ignore plasmamen/robotic species
						continue

					for(var/BP in PP.bodyparts)
						var/obj/item/bodypart/NN = BP
						if(NN.status == BODYPART_ORGANIC && NN.species_id != "plasmaman") //getting every organic, non-plasmaman limb (augments/androids are immune to this)
							plasma_parts += NN
						if(NN.status == BODYPART_ROBOTIC)
							robo_parts += NN

					if(prob(35)) //checking if the delay is over & if the victim actually has any parts to nom
						PP.adjustToxLoss(15)
						PP.adjustFireLoss(25)
						if(plasma_parts.len)
							var/obj/item/bodypart/NB = pick(plasma_parts) //using the above-mentioned list to get a choice of limbs for dismember() to use
							PP.emote("scream")
							NB.species_id = "plasmaman"//change the species_id of the limb to that of a plasmaman
							NB.no_update = TRUE
							NB.change_bodypart_status()
							PP.visible_message("<span class='warning'>[L] screams in pain as [L.p_their()] [NB] melts down to the bone!</span>", \
											  "<span class='userdanger'>You scream out in pain as your [NB] melts down to the bone, leaving an eerie plasma-like glow where flesh used to be!</span>")
						if(!plasma_parts.len && !robo_parts.len) //a person with no potential organic limbs left AND no robotic limbs, time to turn them into a plasmaman
							PP.IgniteMob()
							PP.set_species(/datum/species/plasmaman)
							PP.visible_message("<span class='warning'>[L] bursts into a brilliant purple flame as [L.p_their()] entire body is that of a skeleton!</span>", \
											  "<span class='userdanger'>Your senses numb as all of your remaining flesh is turned into a purple slurry, sloshing off your body and leaving only your bones to show in a vibrant purple!</span>")


/obj/vehicle/ridden/lavaboat/plasma
	name = "plasma boat"
	desc = "A boat used for traversing the streams of plasma without turning into an icecube."
	icon_state = "goliath_boat"
	icon = 'icons/obj/lavaland/dragonboat.dmi'
	resistance_flags = FREEZE_PROOF
	can_buckle = TRUE
///////////	papers


/obj/item/paper/crumpled/ruins/snowdin/foreshadowing
	name = "scribbled note"
	info = {"Something's gone VERY wrong here. Jouslen has been mumbling about some weird shit in his cabin during the night and he seems always tired when we're working. I tried to confront him about it and he blew up on me,
	 telling me to mind my own business. I reported him to the officer, said he'd look into it. We only got another 2 months here before we're pulled for another assignment, so this shit can't go any quicker..."}

/obj/item/paper/crumpled/ruins/snowdin/misc1
	name = "Mission Prologue"
	info = {"Holy shit, what a rush! Those Nanotrasen bastards didn't even know what hit 'em! All five of us dropped in right on the captain, didn't even have time to yell! We were in and out with that disk in mere minutes!
	Crew didn't even know what was happening till the delta alert went down and by then we were already gone. We got a case to drink on the way home to celebrate, fuckin' job well done!"}

/obj/item/paper/crumpled/ruins/snowdin/dontdeadopeninside
	name = "scribbled note"
	info = {"If you're reading this: GET OUT! The mining go on here has unearthed something that was once-trapped by the layers of ice on this hell-hole. The overseer and Jouslen have gone missing. The officer is
	 keeping the rest of us on lockdown and I swear to god I keep hearing strange noises outside the walls at night. The gateway link has gone dead and without a supply of resources from Central, we're left
	 for dead here. We haven't heard anything back from the mining squad either, so I can only assume whatever the fuck they unearthed got them first before coming for us. I don't want to die here..."}

/obj/item/paper/fluff/awaymissions/snowdin/saw_usage
	name = "SAW Usage"
	info = "YOU SEEN IVAN, WHEN YOU HOLD SAAW LIKE PEESTOL, YOU STRONGER THAN RECOIL FOR FEAR OF HITTING FACE!"

/obj/item/paper/fluff/awaymissions/snowdin/research_feed
	name = "Research Feed"
	info = {"<i>A page full of graphs and other detailed information on the seismic activity of the surrounding area.</i>"}

//profile of each of the old crewmembers for the outpost

/obj/item/paper/fluff/awaymissions/snowdin/profile/overseer
	name = "Personnel Record AOP#01"
	info = {"<b><center>Personnel Log</b></center><br><br><b>Name:</b>Caleb Reed<br><b>Age:</b>38<br><b>Gender:</b>Male<br><b>On-Site Profession:</b>Outpost Overseer<br><br><center><b>Information</b></center><br><center>Caleb Reed lead several expeditions
	 among uncharted planets in search of plasma for Nanotrasen, scouring from hot savanas to freezing arctics. Track record is fairly clean with only incidient including the loss of two researchers during the
	 expedition of <b>_______</b>, where mis-used of explosive ordinance for tunneling causes a cave-in."}

/obj/item/paper/fluff/awaymissions/snowdin/profile/sec1
	name = "Personnel Record AOP#02"
	info = {"<b><center>Personnel Log</b></center><br><br><b>Name:</b>James Reed<br><b>Age:</b>43<br><b>Gender:</b>Male<br><b>On-Site Profession:</b>Outpost Security<br><br><center><b>Information</b></center><br><center>James Reed has been a part
	 of Nanotrasen's security force for over 20 years, first joining in 22XX. A clean record and unwavering loyalty to the corperation through numerous deployments to various sites makes him a valuable asset to Natotrasen
	  when it comes to keeping the peace while prioritizing Nanotrasen privacy matters. "}

/obj/item/paper/fluff/awaymissions/snowdin/profile/hydro1
	name = "Personnel Record AOP#03"
	info = {"<b><center>Personnel Log</b></center><br><br><b>Name:</b>Katherine Esterdeen<br><b>Age:</b>27<br><b>Gender:</b>Female<br><b>On-Site Profession:</b>Outpost Botanist<br><br><center><b>Information</b></center><br><center>Katherine Esterdeen is a recent
	 graduate with a major in Botany and a PH.D in Ecology. Having a clean record and eager to work, Esterdeen seems to be the right fit for maintaining plants in the middle of nowhere."}

/obj/item/paper/fluff/awaymissions/snowdin/profile/engi1
	name = "Personnel Record AOP#04"
	info = {"<b><center>Personnel Log</b></center><br><br><b>Name:</b>Rachel Migro<br><b>Age:</b>35<br><b>Gender:</b>Female<br><b>On-Site Profession:</b>Outpost Engineer<br><br><center><b>Information</b></center><br><center>Recently certified to be a full-time Journeyman, Rachel has
		been assigned various construction projects in the past 5 years. Competent and has no past infractions, should be of little concern."}

/obj/item/paper/fluff/awaymissions/snowdin/profile/research1
	name = "Personnel Record AOP#05"
	info = {"<b><center>Personnel Log</b></center><br><br><b>Name:</b>Jacob Ullman<br><b>Age:</b>27<br><b>Gender:</b>Male<br><b>On-Site Profession:</b>Outpost Researcher<br><br><center><b>Information</b></center><br><center>"}

/obj/item/paper/fluff/awaymissions/snowdin/profile/research2
	name = "Personnel Record AOP#06"
	info = {"<b><center>Personnel Log</b></center><br><br><b>Name:</b>Elizabeth Queef<br><b>Age:</b>28<br><b>Gender:</b>Female<br><b>On-Site Profession:</b>Outpost Researcher<br><br><center><b>Information</b></center><br><center>"}

/obj/item/paper/fluff/awaymissions/snowdin/profile/research3
	name = "Personnel Record AOP#07"
	info = {"<b><center>Personnel Log</b></center><br><br><b>Name:</b>Jouslen McGee<br><b>Age:</b>38<br><b>Gender:</b>Male<br><b>On-Site Profession:</b>Outpost Researcher<br><br><center><b>Information</b></center><br><center>"}

/obj/item/paper/fluff/awaymissions/snowdin/secnotice
	name = "Security Notice"
	info = {"YOu have been assigned to this Arctic Post with intention of protecting Nanotrasen assets and ensuring vital information is kept secure while the stationed crew obeys protocol. The picked
		staff for this post have been pre-screened with no prior incidients on record, but incase of an issue you have been given a single holding cell and instructions to contact Central to terminate the
		offending crewmember."}

/obj/item/paper/fluff/awaymissions/snowdin/mining
	name = "Assignment Notice"
	info = {"This cold-ass planet is the new-age equivalent of striking gold. Huge deposits of plasma and literal streams of plasma run through the caverns under all this ice and we're here to mine it all.\
	 Nanotrasen pays by the pound, so get minin' boys!"}

/obj/item/paper/crumpled/ruins/snowdin/lootstructures
	name = "scribbled note"
	info = {"There's some ruins scattered along the cavern, their walls seem to be made of some sort of super-condensed mixture of ice and snow. We've already barricaded up the ones we've found so far,
	 since we keep hearing some strange noises from inside. Besides, what sort of fool would wrecklessly run into ancient ruins full of monsters for some old gear, anyway?"}

/obj/item/paper/crumpled/ruins/snowdin/shovel
	name = "shoveling duties"
	info = {"Snow piles up bad here all-year round, even worse during the winter months. Keeping a constant rotation of shoveling that shit out of the way of the airlocks and keeping the paths decently clear
	is a good step towards not getting stuck walking through knee-deep snow."}

//holo disk recording//--

/obj/item/disk/holodisk/snowdin/weregettingpaidright
	name = "Conversation #AOP#23"
	preset_image_type = /datum/preset_holoimage/researcher
	preset_record_text = {"
	NAME Jacob Ullman
	DELAY 10
	SAY Have you gotten anything interesting on the scanners yet? The deep-drilling from the plasma is making it difficult to get anything that isn't useless noise.
	DELAY 45
	NAME Elizabeth Queef
	DELAY 10
	SAY Nah. I've been feeding the AI the results for the past 2 weeks to sift through the garbage and haven't seen anything out of the usual, at least whatever Nanotrasen is looking for.
	DELAY 45
	NAME Jacob Ullman
	DELAY 10
	SAY Figured as much. Dunno what Nanotrasen expects to find out here past the plasma. At least we're getting paid to fuck around for a couple months while the AI does the hard work.
	DELAY 45
	NAME Elizabeth Queef
	DELAY 10
	SAY . . .
	DELAY 10
	SAY ..We're getting paid?
	DELAY 20
	NAME Jacob Ullman
	DELAY 10
	SAY ..We are getting paid, aren't we..?
	DELAY 15
	PRESET /datum/preset_holoimage/captain
	NAME Caleb Reed
	DELAY 10
	SAY Paid in experience! That's the Nanotrasen Motto!
	DELAY 30;"}

/obj/item/disk/holodisk/snowdin/welcometodie
	name = "Conversation #AOP#1"
	preset_image_type = /datum/preset_holoimage/corgi
	preset_record_text = {"
	NAME Friendly AI Unit
	DELAY 10
	SAY Hello! Welcome to the Arctic Post *338-3**$$!
	DELAY 30
	SAY You have been selected out of $)@! potential candidates for this post!
	DELAY 30
	SAY Nanotrasen is pleased to have you working in one of the many top-of-the-line research posts within the $%@!! sector!
	DELAY 30
	SAY Further job assignment information can be found at your local security post! Have a secure day!
	DELAY 20;"}

/obj/item/disk/holodisk/snowdin/overrun
	name = "Conversation #AOP#55"
	preset_image_type = /datum/preset_holoimage/nanotrasenprivatesecurity
	preset_record_text = {"
	NAME James Reed
	DELAY 10
	SAY Jesus christ, what is that thing??
	DELAY 30
	PRESET /datum/preset_holoimage/researcher
	NAME Elizabeth Queef
	DELAY 10
	SAY Hell if I know! Just shoot it already!
	DELAY 30
	PRESET /datum/preset_holoimage/nanotrasenprivatesecurity
	NAME James Reed
	DELAY 10
	SOUND sound/weapons/laser.ogg
	DELAY 10
	SOUND sound/weapons/laser.ogg
	DELAY 10
	SOUND sound/weapons/laser.ogg
	DELAY 10
	SOUND sound/weapons/laser.ogg
	DELAY 15
	SAY Just go! I'll keep it busy, there's an outpost south of here with an elevator to the surface.
	NAME Jacob Ullman
	PRESET /datum/preset_holoimage/researcher.
	DELAY 15
	Say I don't have to be told twice! Let's get the fuck out of here.
	DELAY 20;"}

/obj/item/disk/holodisk/snowdin/ripjacob
	name = "Conversation #AOP#62"
	preset_image_type = /datum/preset_holoimage/researcher
	preset_record_text = {"
	NAME Jacob Ullman
	DELAY 10
	SAY Get the elevator called. We got no idea how many of those fuckers are down here and I'd rather get off this planet as soon as possible.
	DELAY 45
	NAME Elizabeth Queef
	DELAY 10
	SAY You don't need to tell me twice, I just need to swipe access and then..
	DELAY 15
	SOUND sound/effects/glassbr1.ogg
	DELAY 10
	SOUND sound/effects/glassbr2.ogg
	DELAY 15
	NAME Jacob Ullman
	DELAY 10
	SAY What the FUCK was that?
	DELAY 20
	SAY OH FUCK THERE'S MORE OF THEM. CALL FASTER JESUS CHRIST.
	DELAY 20
	NAME Elizabeth Queef
	DELAY 10
	SAY DON'T FUCKING RUSH ME ALRIGHT IT'S BEING CALLED.
	DELAY 15
	SOUND sound/effects/huuu.ogg
	DELAY 5
	SOUND sound/effects/huuu.ogg
	DELAY 15
	SOUND sound/effects/woodhit.ogg
	DELAY 2
	SOUND sound/effects/bodyfall3.ogg
	DELAY 5
	SOUND sound/effects/meow1.ogg
	DELAY 15
	NAME Jacob Ullman
	DELAY 15
	SAY OH FUCK IT'S GOT ME JESUS CHRIIIiiii-
	NAME Elizabeth Queef
	SAY AAAAAAAAAAAAAAAA FUCK THAT
	DELAY 15;"}

//lootspawners//--

/obj/effect/spawner/lootdrop/snowdin
	name = "why are you using this dummy"
	lootdoubles = 0
	lootcount = 1
	loot = list(/obj/item/bikehorn = 100)

/obj/effect/spawner/lootdrop/snowdin/dungeonlite
	name = "dungeon lite"
	loot = list(/obj/item/melee/classic_baton = 11,
				/obj/item/melee/classic_baton/telescopic = 12,
				/obj/item/book/granter/spell/smoke = 10,
				/obj/item/book/granter/spell/blind = 10,
				/obj/item/storage/firstaid/regular = 45,
				/obj/item/storage/firstaid/toxin = 35,
				/obj/item/storage/firstaid/brute = 27,
				/obj/item/storage/firstaid/fire = 27,
				/obj/item/storage/toolbox/syndicate = 12,
				/obj/item/grenade/c4 = 7,
				/obj/item/grenade/clusterbuster/smoke = 15,
				/obj/item/clothing/under/chameleon = 13,
				/obj/item/clothing/shoes/chameleon/noslip = 10,
				/obj/item/borg/upgrade/ddrill = 3,
				/obj/item/borg/upgrade/soh = 3)

/obj/effect/spawner/lootdrop/snowdin/dungeonmid
	name = "dungeon mid"
	loot = list(/obj/item/defibrillator/compact = 6,
				/obj/item/storage/firstaid/tactical = 35,
				/obj/item/shield/energy = 6,
				/obj/item/shield/riot/tele = 12,
				/obj/item/dnainjector/lasereyesmut = 7,
				/obj/item/gun/magic/wand/fireball/inert = 3,
				/obj/item/pneumatic_cannon = 15,
				/obj/item/melee/transforming/energy/sword = 7,
				/obj/item/book/granter/spell/knock = 15,
				/obj/item/book/granter/spell/summonitem = 20,
				/obj/item/book/granter/spell/forcewall = 17,
				/obj/item/storage/backpack/holding = 12,
				/obj/item/grenade/spawnergrenade/manhacks = 6,
				/obj/item/grenade/spawnergrenade/spesscarp = 7,
				/obj/item/grenade/clusterbuster/inferno = 3,
				/obj/item/stack/sheet/mineral/diamond{amount = 15} = 10,
				/obj/item/stack/sheet/mineral/uranium{amount = 15} = 10,
				/obj/item/stack/sheet/mineral/plasma{amount = 15} = 10,
				/obj/item/stack/sheet/mineral/gold{amount = 15} = 10,
				/obj/item/book/granter/spell/barnyard = 4,
				/obj/item/pickaxe/drill/diamonddrill = 6,
				/obj/item/borg/upgrade/disablercooler = 7)


/obj/effect/spawner/lootdrop/snowdin/dungeonheavy
	name = "dungeon heavy"
	loot = list(/obj/item/twohanded/singularityhammer = 25,
				/obj/item/twohanded/mjollnir = 10,
				/obj/item/twohanded/fireaxe = 25,
				/obj/item/organ/brain/alien = 17,
				/obj/item/twohanded/dualsaber = 15,
				/obj/item/organ/heart/demon = 7,
				/obj/item/gun/ballistic/automatic/c20r/unrestricted = 16,
				/obj/item/gun/magic/wand/resurrection/inert = 15,
				/obj/item/gun/magic/wand/resurrection = 10,
				/obj/item/uplink/old = 2,
				/obj/item/book/granter/spell/charge = 12,
				/obj/item/grenade/clusterbuster/spawner_manhacks = 15,
				/obj/item/book/granter/spell/fireball = 10,
				/obj/item/pickaxe/drill/jackhammer = 30,
				/obj/item/borg/upgrade/syndicate = 13,
				/obj/item/borg/upgrade/selfrepair = 17)

/obj/effect/spawner/lootdrop/snowdin/dungeonmisc
	name = "dungeon misc"
	lootdoubles = 2
	lootcount = 1

	loot = list(/obj/item/stack/sheet/mineral/snow{amount = 25} = 10,
				/obj/item/toy/snowball = 15,
				/obj/item/shovel = 10,
				/obj/item/twohanded/spear = 8,
				)

//special items//--

/obj/structure/barricade/wooden/snowed
	name = "crude plank barricade"
	desc = "This space is blocked off by a wooden barricade. It seems to be covered in a layer of snow."
	icon_state = "woodenbarricade-snow"
	max_integrity = 125

/obj/item/clothing/under/syndicate/coldres
	name = "insulated tactical turtleneck"
	desc = "A nondescript and slightly suspicious-looking turtleneck with digital camouflage cargo pants. The interior has been padded with special insulation for both warmth and protection."
	armor = list("melee" = 20, "bullet" = 10, "laser" = 0,"energy" = 5, "bomb" = 0, "bio" = 0, "rad" = 0, "fire" = 25, "acid" = 25)
	cold_protection = CHEST|GROIN|ARMS|LEGS
	min_cold_protection_temperature = FIRE_SUIT_MIN_TEMP_PROTECT

/obj/item/clothing/shoes/combat/coldres
	name = "insulated combat boots"
	desc = "High speed, low drag combat boots, now with an added layer of insulation."
	min_cold_protection_temperature = FIRE_SUIT_MIN_TEMP_PROTECT

/obj/item/gun/magic/wand/fireball/inert
	name = "weakened wand of fireball"
	desc = "This wand shoots scorching balls of fire that explode into destructive flames. The years of the cold have weakened the magic inside the wand."
	max_charges = 4

/obj/item/gun/magic/wand/resurrection/inert
	name = "weakened wand of healing"
	desc = "This wand uses healing magics to heal and revive. The years of the cold have weakened the magic inside the wand."
	max_charges = 5

/obj/effect/mob_spawn/human/syndicatesoldier/coldres
	name = "Syndicate Snow Operative"
	outfit = /datum/outfit/snowsyndie/corpse

/datum/outfit/snowsyndie/corpse
	name = "Syndicate Snow Operative Corpse"
	implants = null

/obj/effect/mob_spawn/human/syndicatesoldier/coldres/alive
	name = "sleeper"
	mob_name = "Syndicate Snow Operative"
	icon = 'icons/obj/machines/sleeper.dmi'
	icon_state = "sleeper"
	roundstart = FALSE
	death = FALSE
	faction = ROLE_SYNDICATE
	outfit = /datum/outfit/snowsyndie
	short_desc = "You are a syndicate operative recently awoken from cryostasis in an underground outpost."
	flavour_text = "You are a syndicate operative recently awoken from cryostasis in an underground outpost. Monitor Nanotrasen communications and record information. All intruders should be \
	disposed of swiftly to assure no gathered information is stolen or lost. Try not to wander too far from the outpost as the caves can be a deadly place even for a trained operative such as yourself."

/datum/outfit/snowsyndie
	name = "Syndicate Snow Operative"
	uniform = /obj/item/clothing/under/syndicate/coldres
	shoes = /obj/item/clothing/shoes/combat/coldres
	ears = /obj/item/radio/headset/syndicate/alt
	r_pocket = /obj/item/gun/ballistic/automatic/pistol
	id = /obj/item/card/id/syndicate
	implants = list(/obj/item/implant/exile)


/obj/effect/mob_spawn/human/syndicatesoldier/coldres/alive/female
	mob_gender = FEMALE

//mobs//--

//ice spiders moved to giant_spiders.dm

//objs//--

/obj/structure/flora/rock/icy
	name = "icy rock"
	color = rgb(204,233,235)

/obj/structure/flora/rock/pile/icy
	name = "icey rocks"
	color = rgb(204,233,235)

//decals//--
/obj/effect/turf_decal/snowdin_station_sign
	icon_state = "AOP1"

/obj/effect/turf_decal/snowdin_station_sign/two
	icon_state = "AOP2"

/obj/effect/turf_decal/snowdin_station_sign/three
	icon_state = "AOP3"

/obj/effect/turf_decal/snowdin_station_sign/four
	icon_state = "AOP4"

/obj/effect/turf_decal/snowdin_station_sign/five
	icon_state = "AOP5"

/obj/effect/turf_decal/snowdin_station_sign/six
	icon_state = "AOP6"

/obj/effect/turf_decal/snowdin_station_sign/seven
	icon_state = "AOP7"

/obj/effect/turf_decal/snowdin_station_sign/up
	icon_state = "AOPU1"

/obj/effect/turf_decal/snowdin_station_sign/up/two
	icon_state = "AOPU2"

/obj/effect/turf_decal/snowdin_station_sign/up/three
	icon_state = "AOPU3"

/obj/effect/turf_decal/snowdin_station_sign/up/four
	icon_state = "AOPU4"

/obj/effect/turf_decal/snowdin_station_sign/up/five
	icon_state = "AOPU5"

/obj/effect/turf_decal/snowdin_station_sign/up/six
	icon_state = "AOPU6"

/obj/effect/turf_decal/snowdin_station_sign/up/seven
	icon_state = "AOPU7"
