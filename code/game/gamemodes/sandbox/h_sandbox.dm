GLOBAL_VAR_INIT(hsboxspawn, TRUE)

/mob/proc/CanBuild()
	sandbox = new/datum/hSB
	sandbox.owner = src.ckey
	if(src.client.holder)
		sandbox.admin = 1
	verbs += new/mob/proc/sandbox_panel
/mob/proc/sandbox_panel()
	set name = "Sandbox Panel"
	if(sandbox)
		sandbox.update()

/datum/hSB
	var/owner = null
	var/admin = 0

	var/static/clothinfo = null
	var/static/reaginfo = null
	var/static/objinfo = null
	var/canisterinfo = null
	var/hsbinfo = null
	//items that shouldn't spawn on the floor because they would bug or act weird
	var/static/list/spawn_forbidden = list(
		/obj/item/tk_grab, /obj/item/implant, // not implanter, the actual thing that is inside you
		/obj/item/assembly, /obj/item/onetankbomb, /obj/item/pda/ai,
		/obj/item/smallDelivery, /obj/projectile,
		/obj/item/borg/sight, /obj/item/borg/stun, /obj/item/robot_module)

/datum/hSB/proc/update()
	var/static/list/hrefs = list(
			"Space Gear",
			"Suit Up (Space Travel Gear)"		= "hsbsuit",
			"Spawn Gas Mask"					= "hsbspawn&path=[/obj/item/clothing/mask/gas]",
			"Spawn Emergency Air Tank"			= "hsbspawn&path=[/obj/item/tank/internals/emergency_oxygen/double]",

			"Standard Tools",
			"Spawn Flashlight"					= "hsbspawn&path=[/obj/item/flashlight]",
			"Spawn Toolbox"						= "hsbspawn&path=[/obj/item/storage/toolbox/mechanical]",
			"Spawn Experimental Welding tool"	= "hsbspawn&path=[/obj/item/weldingtool/experimental]",
			"Spawn Light Replacer"				= "hsbspawn&path=[/obj/item/lightreplacer]",
			"Spawn Medical Kit"					= "hsbspawn&path=[/obj/item/storage/firstaid/regular]",
			"Spawn All-Access ID"				= "hsbaaid",

			"Building Supplies",
			"Spawn 50 Wood"                     = "hsbwood",
			"Spawn 50 Metal"					= "hsbmetal",
			"Spawn 50 Plasteel"					= "hsbplasteel",
			"Spawn 50 Reinforced Glass"         = "hsbrglass",
			"Spawn 50 Glass"					= "hsbglass",
			"Spawn Box of Materials"			= "hsbspawn&path=[/obj/item/storage/box/material]",
			"Spawn Full Cable Coil"				= "hsbspawn&path=[/obj/item/stack/cable_coil]",
			"Spawn Hyper Capacity Power Cell"	= "hsbspawn&path=[/obj/item/stock_parts/cell/hyper]",
			"Spawn Inf. Capacity Power Cell"	= "hsbspawn&path=[/obj/item/stock_parts/cell/infinite]",
			"Spawn Rapid Construction Device"	= "hsbrcd",
			"Spawn RCD Ammo"					= "hsb_safespawn&path=[/obj/item/rcd_ammo]",
			"Spawn Airlock"						= "hsbairlock",

			"Miscellaneous",
			"Spawn Air Scrubber"				= "hsbscrubber",
			"Spawn CentCom Technology Disk"		= "hsbspawn&path=[/obj/item/disk/tech_disk/debug]",
			"Spawn Adminordrazine"				= "hsbspawn&path=[/obj/item/reagent_containers/pill/adminordrazine]",
			"Spawn Water Tank"					= "hsbspawn&path=[/obj/structure/reagent_dispensers/watertank]",

			"Bots",
			"Spawn Cleanbot"					= "hsbspawn&path=[/mob/living/simple_animal/bot/cleanbot]",
			"Spawn Floorbot"					= "hsbspawn&path=[/mob/living/simple_animal/bot/floorbot]",
			"Spawn Medbot"						= "hsbspawn&path=[/mob/living/simple_animal/bot/medbot]",

			"Canisters",
			"Spawn O2 Canister" 				= "hsbspawn&path=[/obj/machinery/portable_atmospherics/canister/oxygen]",
			"Spawn Air Canister"				= "hsbspawn&path=[/obj/machinery/portable_atmospherics/canister/air]")


	if(!hsbinfo)
		hsbinfo = "<center><b>Sandbox Panel</b></center><hr>"
		if(admin)
			hsbinfo += "<b>Administration</b><br>"
			hsbinfo += "- <a href='?src=[REF(src)];hsb=hsbtobj'>Toggle Object Spawning</a><br>"
			hsbinfo += "- <a href='?src=[REF(src)];hsb=hsbtac'>Toggle Item Spawn Panel Auto-close</a><br>"
			hsbinfo += "<b>Canister Spawning</b><br>"
			hsbinfo += "- <a href='?src=[REF(src)];hsb=hsbspawn&path=[/obj/machinery/portable_atmospherics/canister/toxins]'>Spawn Plasma Canister</a><br>"
			hsbinfo += "- <a href='?src=[REF(src)];hsb=hsbspawn&path=[/obj/machinery/portable_atmospherics/canister/carbon_dioxide]'>Spawn CO2 Canister</a><br>"
			hsbinfo += "- <a href='?src=[REF(src)];hsb=hsbspawn&path=[/obj/machinery/portable_atmospherics/canister/nitrogen]'>Spawn Nitrogen Canister</a><br>"
			hsbinfo += "- <a href='?src=[REF(src)];hsb=hsbspawn&path=[/obj/machinery/portable_atmospherics/canister/nitrous_oxide]'>Spawn N2O Canister</a><hr>"
		else
			hsbinfo += "<i>Some item spawning may be disabled by the administrators.</i><br>"
			hsbinfo += "<i>Only administrators may spawn dangerous canisters.</i><br>"
		for(var/T in hrefs)
			var/href = hrefs[T]
			if(href)
				hsbinfo += "- <a href='?[REF(src)];hsb=[hrefs[T]]'>[T]</a><br>"
			else
				hsbinfo += "<br><b>[T]</b><br>"
		hsbinfo += "<hr>"
		hsbinfo += "- <a href='?[REF(src)];hsb=hsbcloth'>Spawn Clothing...</a><br>"
		hsbinfo += "- <a href='?[REF(src)];hsb=hsbreag'>Spawn Reagent Container...</a><br>"
		hsbinfo += "- <a href='?[REF(src)];hsb=hsbobj'>Spawn Other Item...</a><br><br>"

	usr << browse(hsbinfo, "window=hsbpanel")

/datum/hSB/Topic(href, href_list)
	if(!usr || !src || !(src.owner == usr.ckey))
		if(usr)
			usr << browse(null,"window=sandbox")
		return

	if(href_list["hsb"])
		switch(href_list["hsb"])
			//
			// Admin: toggle spawning
			//
			if("hsbtobj")
				if(!admin) return
				if(GLOB.hsboxspawn)
					to_chat(world, "<span class='boldannounce'>Sandbox:</span> <b>\black[usr.key] has disabled object spawning!</b>")
					GLOB.hsboxspawn = FALSE
					return
				else
					to_chat(world, "<span class='boldnotice'>Sandbox:</span> <b>\black[usr.key] has enabled object spawning!</b>")
					GLOB.hsboxspawn = TRUE
					return
			//
			// Admin: Toggle auto-close
			//
			if("hsbtac")
				if(!admin) return
				var/sbac = CONFIG_GET(flag/sandbox_autoclose)
				if(sbac)
					to_chat(world, "<span class='boldnotice'>Sandbox:</span> <b>\black [usr.key] has removed the object spawn limiter.</b>")
				else
					to_chat(world, "<span class='danger'>Sandbox:</span> <b>\black [usr.key] has added a limiter to object spawning. The window will now auto-close after use.</b>")
				CONFIG_SET(flag/sandbox_autoclose, !sbac)
				return
			//
			// Spacesuit with full air jetpack set as internals
			//
			if("hsbsuit")
				var/mob/living/carbon/human/P = usr
				if(!istype(P)) return
				if(P.wear_suit)
					P.wear_suit.forceMove(P.drop_location())
					P.wear_suit.layer = initial(P.wear_suit.layer)
					P.wear_suit.plane = initial(P.wear_suit.plane)
					P.wear_suit = null
				P.wear_suit = new/obj/item/clothing/suit/space(P)
				P.wear_suit.layer = ABOVE_HUD_LAYER
				P.wear_suit.plane = ABOVE_HUD_PLANE
				P.update_inv_wear_suit()
				if(P.head)
					P.head.forceMove(P.drop_location())
					P.head.layer = initial(P.head.layer)
					P.head.plane = initial(P.head.plane)
					P.head = null
				P.head = new/obj/item/clothing/head/helmet/space(P)
				P.head.layer = ABOVE_HUD_LAYER
				P.head.plane = ABOVE_HUD_PLANE
				P.update_inv_head()
				if(P.wear_mask)
					P.wear_mask.forceMove(P.drop_location())
					P.wear_mask.layer = initial(P.wear_mask.layer)
					P.wear_mask.plane = initial(P.wear_mask.plane)
					P.wear_mask = null
				P.wear_mask = new/obj/item/clothing/mask/gas(P)
				P.wear_mask.layer = ABOVE_HUD_LAYER
				P.wear_mask.plane = ABOVE_HUD_PLANE
				P.update_inv_wear_mask()
				if(P.back)
					P.back.forceMove(P.drop_location())
					P.back.layer = initial(P.back.layer)
					P.back.plane = initial(P.back.plane)
					P.back = null
				P.back = new/obj/item/tank/jetpack/oxygen(P)
				P.back.layer = ABOVE_HUD_LAYER
				P.back.plane = ABOVE_HUD_PLANE
				P.update_inv_back()
				P.internal = P.back
				P.update_internals_hud_icon(1)

			if("hsbscrubber") // This is beyond its normal capability but this is sandbox and you spawned one, I assume you need it
				var/obj/hsb = new/obj/machinery/portable_atmospherics/scrubber{volume_rate=50*ONE_ATMOSPHERE;on=1}(usr.loc)
				hsb.update_icon() // hackish but it wasn't meant to be spawned I guess?

			//
			// Stacked Materials
			//

			if("hsbrglass")
				new/obj/item/stack/sheet/rglass{amount=50}(usr.loc)

			if("hsbmetal")
				new/obj/item/stack/sheet/metal{amount=50}(usr.loc)

			if("hsbplasteel")
				new/obj/item/stack/sheet/plasteel{amount=50}(usr.loc)

			if("hsbglass")
				new/obj/item/stack/sheet/glass{amount=50}(usr.loc)

			if("hsbwood")
				new/obj/item/stack/sheet/mineral/wood{amount=50}(usr.loc)

			//
			// All access ID
			//
			if("hsbaaid")
				var/obj/item/card/id/gold/ID = new(usr.loc)
				ID.registered_name = usr.real_name
				ID.assignment = "Sandbox"
				ID.access = get_all_accesses()
				ID.update_label()

			//
			// RCD - starts with full clip
			// Spawn check due to grief potential (destroying floors, walls, etc)
			//
			if("hsbrcd")
				if(!GLOB.hsboxspawn) return

				new/obj/item/construction/rcd/combat(usr.loc)

			//
			// New sandbox airlock maker
			//
			if("hsbairlock")
				new /datum/airlock_maker(usr.loc)

			//
			// Object spawn window
			//

			// Clothing
			if("hsbcloth")
				if(!GLOB.hsboxspawn) return

				if(!clothinfo)
					clothinfo = "<b>Clothing</b> <a href='?[REF(src)];hsb=hsbreag'>(Reagent Containers)</a> <a href='?[REF(src)];hsb=hsbobj'>(Other Items)</a><hr><br>"
					var/list/all_items = subtypesof(/obj/item/clothing)
					for(var/typekey in spawn_forbidden)
						all_items -= typesof(typekey)
					for(var/O in reverseRange(all_items))
						clothinfo += "<a href='?src=[REF(src)];hsb=hsb_safespawn&path=[O]'>[O]</a><br>"

				usr << browse(clothinfo,"window=sandbox")

			// Reagent containers
			if("hsbreag")
				if(!GLOB.hsboxspawn) return

				if(!reaginfo)
					reaginfo = "<b>Reagent Containers</b> <a href='?[REF(src)];hsb=hsbcloth'>(Clothing)</a> <a href='?[REF(src)];hsb=hsbobj'>(Other Items)</a><hr><br>"
					var/list/all_items = subtypesof(/obj/item/reagent_containers)
					for(var/typekey in spawn_forbidden)
						all_items -= typesof(typekey)
					for(var/O in reverseRange(all_items))
						reaginfo += "<a href='?src=[REF(src)];hsb=hsb_safespawn&path=[O]'>[O]</a><br>"

				usr << browse(reaginfo,"window=sandbox")

			// Other items
			if("hsbobj")
				if(!GLOB.hsboxspawn) return

				if(!objinfo)
					objinfo = "<b>Other Items</b> <a href='?[REF(src)];hsb=hsbcloth'>(Clothing)</a> <a href='?[REF(src)];hsb=hsbreag'>(Reagent Containers)</a><hr><br>"
					var/list/all_items = subtypesof(/obj/item/) - typesof(/obj/item/clothing) - typesof(/obj/item/reagent_containers)
					for(var/typekey in spawn_forbidden)
						all_items -= typesof(typekey)

					for(var/O in reverseRange(all_items))
						objinfo += "<a href='?src=[REF(src)];hsb=hsb_safespawn&path=[O]'>[O]</a><br>"

				usr << browse(objinfo,"window=sandbox")

			//
			// Safespawn checks to see if spawning is disabled.
			//
			if("hsb_safespawn")
				if(!GLOB.hsboxspawn)
					usr << browse(null,"window=sandbox")
					return

				var/typepath = text2path(href_list["path"])
				if(!typepath)
					to_chat(usr, "Bad path: \"[href_list["path"]]\"")
					return
				new typepath(usr.loc)

				if(CONFIG_GET(flag/sandbox_autoclose))
					usr << browse(null,"window=sandbox")
			//
			// For everything else in the href list
			//
			if("hsbspawn")
				var/typepath = text2path(href_list["path"])
				if(!typepath)
					to_chat(usr, "Bad path: \"[href_list["path"]]\"")
					return
				new typepath(usr.loc)

				if(CONFIG_GET(flag/sandbox_autoclose))
					usr << browse(null,"window=sandbox")
