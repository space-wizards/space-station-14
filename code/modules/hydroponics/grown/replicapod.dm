// A very special plant, deserving it's own file.

/obj/item/seeds/replicapod
	name = "pack of replica pod seeds"
	desc = "These seeds grow into replica pods. They say these are used to harvest humans."
	icon_state = "seed-replicapod"
	species = "replicapod"
	plantname = "Replica Pod"
	product = /mob/living/carbon/human //verrry special -- Urist
	lifespan = 50
	endurance = 8
	maturation = 10
	production = 1
	yield = 1 //seeds if there isn't a dna inside
	potency = 30
	var/volume = 5
	var/ckey
	var/realName
	var/datum/mind/mind
	var/blood_gender
	var/blood_type
	var/list/features
	var/factions
	var/list/quirks
	var/sampleDNA
	var/contains_sample = FALSE

/obj/item/seeds/replicapod/Initialize()
	. = ..()

	create_reagents(volume, INJECTABLE|DRAWABLE)

/obj/item/seeds/replicapod/on_reagent_change(changetype)
	if(changetype == ADD_REAGENT)
		var/datum/reagent/blood/B = reagents.has_reagent(/datum/reagent/blood)
		if(B)
			if(B.data["mind"] && B.data["cloneable"])
				mind = B.data["mind"]
				ckey = B.data["ckey"]
				realName = B.data["real_name"]
				blood_gender = B.data["gender"]
				blood_type = B.data["blood_type"]
				features = B.data["features"]
				factions = B.data["factions"]
				quirks = B.data["quirks"]
				sampleDNA = B.data["blood_DNA"]
				contains_sample = TRUE
				visible_message("<span class='notice'>The [src] is injected with a fresh blood sample.</span>")
				log_cloning("[key_name(mind)]'s cloning record was added to [src] at [AREACOORD(src)].")
			else
				visible_message("<span class='warning'>The [src] rejects the sample!</span>")

	if(!reagents.has_reagent(/datum/reagent/blood))
		mind = null
		ckey = null
		realName = null
		blood_gender = null
		blood_type = null
		features = null
		factions = null
		sampleDNA = null
		contains_sample = FALSE

/obj/item/seeds/replicapod/get_analyzer_text()
	var/text = ..()
	if(contains_sample)
		text += "\n It contains a blood sample with blood DNA (UE) \"sampleDNA\"." //blood DNA (UE) shows in medical records and is readable by forensics scanners
	return text

/obj/item/seeds/replicapod/harvest(mob/user) //now that one is fun -- Urist
	var/obj/machinery/hydroponics/parent = loc
	var/make_podman = FALSE
	var/ckey_holder = null
	var/list/result = list()
	if(CONFIG_GET(flag/revival_pod_plants))
		if(ckey)
			for(var/mob/M in GLOB.player_list)
				if(isobserver(M))
					var/mob/dead/observer/O = M
					if(O.ckey == ckey && O.can_reenter_corpse)
						make_podman = TRUE
						break
				else
					if(M.ckey == ckey && M.stat == DEAD && !M.suiciding)
						make_podman = TRUE
						if(isliving(M))
							var/mob/living/L = M
							make_podman = !L.hellbound
						break
		else //If the player has ghosted from his corpse before blood was drawn, his ckey is no longer attached to the mob, so we need to match up the cloned player through the mind key
			for(var/mob/M in GLOB.player_list)
				if(mind && M.mind && ckey(M.mind.key) == ckey(mind.key) && M.ckey && M.client && M.stat == DEAD && !M.suiciding)
					if(isobserver(M))
						var/mob/dead/observer/O = M
						if(!O.can_reenter_corpse)
							break
					make_podman = TRUE
					if(isliving(M))
						var/mob/living/L = M
						make_podman = !L.hellbound
					ckey_holder = M.ckey
					break

	if(make_podman)	//all conditions met!
		var/mob/living/carbon/human/podman = new /mob/living/carbon/human(parent.loc)
		if(realName)
			podman.real_name = realName
		else
			podman.real_name = "Pod Person ([rand(1,999)])"
		mind.transfer_to(podman)
		if(ckey)
			podman.ckey = ckey
		else
			podman.ckey = ckey_holder
		podman.gender = blood_gender
		podman.faction |= factions
		if(!features["mcolor"])
			features["mcolor"] = "#59CE00"
		for(var/V in quirks)
			new V(podman)
		podman.hardset_dna(null,null,podman.real_name,blood_type, new /datum/species/pod,features)//Discard SE's and UI's, podman cloning is inaccurate, and always make them a podman
		podman.set_cloned_appearance()
		log_cloning("[key_name(mind)] cloned as a podman via [src] in [parent] at [AREACOORD(parent)].")

	else //else, one packet of seeds. maybe two
		var/seed_count = 1
		if(prob(getYield() * 20))
			seed_count++
		var/output_loc = parent.Adjacent(user) ? user.loc : parent.loc //needed for TK
		for(var/i=0,i<seed_count,i++)
			var/obj/item/seeds/replicapod/harvestseeds = src.Copy()
			result.Add(harvestseeds)
			harvestseeds.forceMove(output_loc)

	parent.update_tray()
	return result
