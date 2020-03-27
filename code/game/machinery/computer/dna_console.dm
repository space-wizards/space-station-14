#define INJECTOR_TIMEOUT 100
#define NUMBER_OF_BUFFERS 3
#define SCRAMBLE_TIMEOUT 600
#define JOKER_TIMEOUT 12000					//20 minutes
#define JOKER_UPGRADE 3000

#define RADIATION_STRENGTH_MAX 15
#define RADIATION_STRENGTH_MULTIPLIER 1			//larger has more range

#define RADIATION_DURATION_MAX 30
#define RADIATION_ACCURACY_MULTIPLIER 3			//larger is less accurate


#define RADIATION_IRRADIATION_MULTIPLIER 1		//multiplier for how much radiation a test subject receives

#define SCANNER_ACTION_SE 1
#define SCANNER_ACTION_UI 2
#define SCANNER_ACTION_UE 3
#define SCANNER_ACTION_MIXED 4

/obj/machinery/computer/scan_consolenew
	name = "\improper DNA scanner access console"
	desc = "Scan DNA."
	icon_screen = "dna"
	icon_keyboard = "med_key"
	density = TRUE
	circuit = /obj/item/circuitboard/computer/scan_consolenew

	use_power = IDLE_POWER_USE
	idle_power_usage = 10
	active_power_usage = 400
	light_color = LIGHT_COLOR_BLUE

	var/datum/techweb/stored_research
	var/max_storage = 6
	var/combine
	var/radduration = 2
	var/radstrength = 1
	var/max_chromosomes = 6
	///Amount of mutations we can store
	var/list/buffer[NUMBER_OF_BUFFERS]
	///mutations we have stored
	var/list/stored_mutations = list()
	///chromosomes we have stored
	var/list/stored_chromosomes = list()
	///combinations of injectors for the 'injector selection'. format is list("Elsa" = list(Cryokinesis, Geladikinesis), "The Hulk" = list(Hulk, Gigantism), etc) Glowy and the gang being an initialized datum
	var/list/injector_selection = list()
	///max amount of selections you can make
	var/max_injector_selections = 2
	///hard-cap on the advanced dna injector
	var/max_injector_mutations = 10
	///the max instability of the advanced injector.
	var/max_injector_instability = 50

	var/injectorready = 0	//world timer cooldown var
	var/jokerready = 0
	var/scrambleready = 0
	var/current_screen = "mainmenu"
	var/current_mutation   //what block are we inspecting? only used when screen = "info"
	var/current_storage   //what storage block are we looking at?
	var/obj/machinery/dna_scannernew/connected = null
	var/obj/item/disk/data/diskette = null
	var/list/delayed_action = null

/obj/machinery/computer/scan_consolenew/attackby(obj/item/I, mob/user, params)
	if (istype(I, /obj/item/disk/data)) //INSERT SOME DISKETTES
		if (!user.transferItemToLoc(I,src))
			return
		if(diskette)
			diskette.forceMove(drop_location())
			diskette = null
		diskette = I
		to_chat(user, "<span class='notice'>You insert [I].</span>")
		updateUsrDialog()
		return
	if (istype(I, /obj/item/chromosome))
		if(LAZYLEN(stored_chromosomes) < max_chromosomes)
			I.forceMove(src)
			stored_chromosomes += I
			to_chat(user, "<span class='notice'>You insert [I].</span>")
		else
			to_chat(user, "<span class='warning'>You cannot store any more chromosomes!</span>")
		return
	if(istype(I, /obj/item/dnainjector/activator))
		var/obj/item/dnainjector/activator/A = I
		if(A.used)
			to_chat(user,"<span class='notice'>Recycled [I].</span>")
			if(A.research)
				var/c_typepath = generate_chromosome()
				var/obj/item/chromosome/CM = new c_typepath (drop_location())
				to_chat(user,"<span class='notice'>Recycled [I].</span>")
				if((LAZYLEN(stored_chromosomes) < max_chromosomes) && prob(60))
					CM.forceMove(src)
					stored_chromosomes += CM
					to_chat(user,"<span class='notice'>[capitalize(CM.name)] added to storage.</span>")
			qdel(I)
			return

	else
		return ..()

/obj/machinery/computer/scan_consolenew/Initialize()
	. = ..()
	for(var/direction in GLOB.cardinals)
		connected = locate(/obj/machinery/dna_scannernew, get_step(src, direction))
		if(!isnull(connected))
			break
	injectorready = world.time + INJECTOR_TIMEOUT
	scrambleready = world.time + SCRAMBLE_TIMEOUT
	jokerready = world.time + JOKER_TIMEOUT

	stored_research = SSresearch.science_tech

/obj/machinery/computer/scan_consolenew/examine(mob/user)
	. = ..()
	if(jokerready < world.time)
		. += "<span class='notice'>JOKER algorithm available.</span>"
	else
		. += "<span class='notice'>JOKER algorithm available in about [round(0.00166666667 * (jokerready - world.time))] minutes.</span>"

/obj/machinery/computer/scan_consolenew/ui_interact(mob/user, last_change)
	. = ..()
	if(!user)
		return
	var/datum/browser/popup = new(user, "scannernew", "DNA Modifier Console", 800, 630) // Set up the popup browser window
	if(user.client)
		var/datum/asset/simple/assets =  get_asset_datum(/datum/asset/simple/genetics)
		assets.send(user.client)
	if(!(in_range(src, user) || issilicon(user)))
		popup.close()
		return
	popup.add_stylesheet("scannernew", 'html/browser/scannernew.css')

	var/mob/living/carbon/viable_occupant
	var/list/occupant_status = list("<div class='line'><div class='statusLabel'>Subject Status:</div><div class='statusValue'>")
	var/scanner_status
	var/list/temp_html = list()
	if(connected && connected.is_operational())
		if(connected.occupant)	//set occupant_status message
			viable_occupant = connected.occupant
			if(viable_occupant.has_dna() && !HAS_TRAIT(viable_occupant, TRAIT_RADIMMUNE) && !HAS_TRAIT(viable_occupant, TRAIT_BADDNA) || (connected.scan_level == 3)) //occupant is viable for dna modification
				occupant_status += "[viable_occupant.name] => "
				switch(viable_occupant.stat)
					if(CONSCIOUS)
						occupant_status += "<span class='good'>Conscious</span>"
					if(UNCONSCIOUS)
						occupant_status += "<span class='average'>Unconscious</span>"
					else
						occupant_status += "<span class='bad'>DEAD</span>"
				occupant_status += "</div></div>"
				occupant_status += "<div class='line'><div class='statusLabel'>Health:</div><div class='progressBar'><div style='width: [viable_occupant.health]%;' class='progressFill good'></div></div><div class='statusValue'>[viable_occupant.health] %</div></div>"
				occupant_status += "<div class='line'><div class='statusLabel'>Radiation Level:</div><div class='progressBar'><div style='width: [viable_occupant.radiation/(RAD_MOB_SAFE/100)]%;' class='progressFill bad'></div></div><div class='statusValue'>[viable_occupant.radiation/(RAD_MOB_SAFE/100)] %</div></div>"
				occupant_status += "<div class='line'><div class='statusLabel'>Unique Enzymes :</div><div class='statusValue'><span class='highlight'>[viable_occupant.dna.unique_enzymes]</span></div></div>"
				occupant_status += "<div class='line'><div class='statusLabel'>Last Operation:</div><div class='statusValue'>[last_change ? last_change : "----"]</div></div>"
			else
				viable_occupant = null
				occupant_status += "<span class='bad'>Invalid DNA structure</span></div></div>"
		else
			occupant_status += "<span class='bad'>No subject detected</span></div></div>"

		if(connected.state_open)
			scanner_status = "Open"
		else
			scanner_status = "Closed"
			if(connected.locked)
				scanner_status += "<span class='bad'>(Locked)</span>"
			else
				scanner_status += "<span class='good'>(Unlocked)</span>"


	else
		occupant_status += "<span class='bad'>----</span></div></div>"
		scanner_status += "<span class='bad'>Error: No scanner detected</span>"

	var/list/status = list("<div class='statusDisplay'>")
	status += "<div class='line'><div class='statusLabel'>Scanner:</div><div class='statusValue'>[scanner_status]</div></div>"
	status += occupant_status


	status += "<div class='line'><h3>Radiation Emitter Status</h3></div>"
	var/stddev = radstrength*RADIATION_STRENGTH_MULTIPLIER
	status += "<div class='line'><div class='statusLabel'>Output Level:</div><div class='statusValue'>[radstrength]</div></div>"
	status += "<div class='line'><div class='statusLabel'>&nbsp;&nbsp;\> Mutation:</div><div class='statusValue'>(-[stddev] to +[stddev] = 68 %) (-[2*stddev] to +[2*stddev] = 95 %)</div></div>"
	if(connected)
		stddev = RADIATION_ACCURACY_MULTIPLIER/(radduration + (connected.precision_coeff ** 2))
	else
		stddev = RADIATION_ACCURACY_MULTIPLIER/radduration
	var/chance_to_hit
	switch(stddev)	//hardcoded values from a z-table for a normal distribution
		if(0 to 0.25)
			chance_to_hit = ">95 %"
		if(0.25 to 0.5)
			chance_to_hit = "68-95 %"
		if(0.5 to 0.75)
			chance_to_hit = "55-68 %"
		else
			chance_to_hit = "<38 %"
	status += "<div class='line'><div class='statusLabel'>Pulse Duration:</div><div class='statusValue'>[radduration]</div></div>"
	status += "<div class='line'><div class='statusLabel'>&nbsp;&nbsp;\> Accuracy:</div><div class='statusValue'>[chance_to_hit]</div></div>"
	status += "<br></div>" // Close statusDisplay div
	var/list/buttons = list("<a href='?src=[REF(src)];'>Scan</a>")
	if(connected)
		buttons += "<a href='?src=[REF(src)];task=toggleopen;'>[connected.state_open ? "Close" : "Open"] Scanner</a>"
		if (connected.state_open)
			buttons += "<span class='linkOff'>[connected.locked ? "Unlock" : "Lock"] Scanner</span>"
		else
			buttons += "<a href='?src=[REF(src)];task=togglelock;'>[connected.locked ? "Unlock" : "Lock"] Scanner</a>"
	else
		buttons += "<span class='linkOff'>Open Scanner</span> <span class='linkOff'>Lock Scanner</span>"
	if(viable_occupant && (scrambleready < world.time))
		buttons += "<a href='?src=[REF(src)];task=scramble'>Scramble DNA</a>"
	else
		buttons += "<span class='linkOff'>Scramble DNA</span>"
	if(diskette)
		buttons += "<a href='?src=[REF(src)];task=screen;text=disk;'>Disk</a>"
	else
		buttons += "<span class='linkOff'>Disk</span>"
	if(current_screen == "mutations")
		buttons += "<br><span class='linkOff'>Mutations</span>"
	else
		buttons += "<br><a href='?src=[REF(src)];task=screen;text=mutations;'>Mutations</a>"
	if((current_screen == "mainmenu") || !current_screen)
		buttons += "<span class='linkOff'>Genetic Sequencer</span>"
	else
		buttons += "<a href='?src=[REF(src)];task=screen;text=mainmenu;'>Genetic Sequencer</a>"
	if(current_screen == "ui")
		buttons += "<span class='linkOff'>Unique Identifiers</span>"
	else
		buttons += "<a href='?src=[REF(src)];task=screen;text=ui;'>Unique Identifiers</a>"
	if(current_screen == "advinjector")
		buttons += "<span class='linkOff'>Adv. Injectors</span>"
	else
		buttons += "<a href='?src=[REF(src)];task=screen;text=advinjector;'>Adv. Injectors</a>"

	switch(current_screen)
		if("working")
			temp_html += status
			temp_html += "<h1>System Busy</h1>"
			temp_html += "Working ... Please wait ([DisplayTimeText(radduration*10)])"
		if("ui")
			temp_html += status
			temp_html += buttons
			temp_html += "<h1>Unique Identifiers</h1>"
			temp_html += "<a href='?src=[REF(src)];task=setstrength;num=[radstrength-1];'>--</a> <a href='?src=[REF(src)];task=setstrength;'>Output Level</a> <a href='?src=[REF(src)];task=setstrength;num=[radstrength+1];'>++</a>"
			temp_html += "<br><a href='?src=[REF(src)];task=setduration;num=[radduration-1];'>--</a> <a href='?src=[REF(src)];task=setduration;'>Pulse Duration</a> <a href='?src=[REF(src)];task=setduration;num=[radduration+1];'>++</a>"
			temp_html += "<h3>Irradiate Subject</h3>"
			temp_html += "<div class='line'><div class='statusLabel'>Unique Identifier:</div><div class='statusValue'><div class='clearBoth'>"
			var/max_line_len = 7*DNA_BLOCK_SIZE
			if(viable_occupant)
				temp_html += "<div class='dnaBlockNumber'>1</div>"
				var/char = ""
				var/ui_text = viable_occupant.dna.uni_identity
				var/len_byte = length(ui_text)
				var/char_it = 0
				for(var/byte_it = 1, byte_it <= len_byte, byte_it += length(char))
					char_it++
					char = ui_text[byte_it]
					temp_html += "<a class='dnaBlock' href='?src=[REF(src)];task=pulseui;num=[char_it];'>[char]</a>"
					if((char_it % max_line_len) == 0)
						temp_html += "</div><div class='clearBoth'>"
					if((char_it % DNA_BLOCK_SIZE) == 0 && byte_it < len_byte)
						temp_html += "<div class='dnaBlockNumber'>[(char_it / DNA_BLOCK_SIZE) + 1]</div>"
			else
				temp_html += "---------"
			temp_html += "</div></div><br><h1>Buffer Menu</h1>"

			if(istype(buffer))
				for(var/i=1, i<=buffer.len, i++)
					temp_html += "<br>Slot [i]: "
					var/list/buffer_slot = buffer[i]
					if( !buffer_slot || !buffer_slot.len || !buffer_slot["name"] || !((buffer_slot["UI"] && buffer_slot["UE"]) || buffer_slot["SE"]) )
						temp_html += "<br>\tNo Data"
						if(viable_occupant)
							temp_html += "<br><a href='?src=[REF(src)];task=setbuffer;num=[i];'>Save to Buffer</a>"
						else
							temp_html += "<br><span class='linkOff'>Save to Buffer</span>"
						temp_html += "<span class='linkOff'>Clear Buffer</span>"
						if(diskette)
							temp_html += "<a href='?src=[REF(src)];task=loaddisk;num=[i];'>Load from Disk</a>"
						else
							temp_html += "<span class='linkOff'>Load from Disk</span>"
						temp_html += "<span class='linkOff'>Save to Disk</span>"
					else
						var/ui = buffer_slot["UI"]
						var/ue = buffer_slot["UE"]
						var/name = buffer_slot["name"]
						var/label = buffer_slot["label"]
						var/blood_type = buffer_slot["blood_type"]
						temp_html += "<br>\t<a href='?src=[REF(src)];task=setbufferlabel;num=[i];'>Label</a>: [label ? label : name]"
						temp_html += "<br>\tSubject: [name]"
						if(ue && name && blood_type)
							temp_html += "<br>\tBlood Type: [blood_type]"
							temp_html += "<br>\tUE: [ue] "
							if(viable_occupant)
								temp_html += "<a href='?src=[REF(src)];task=transferbuffer;num=[i];text=ue'>Occupant</a>"
							else
								temp_html += "<span class='linkOff'>Occupant</span>"
							temp_html += "<a href='?src=[REF(src)];task=setdelayed;num=[i];delayaction=[SCANNER_ACTION_UE]'>Occupant:Delayed</a>"
							if(injectorready < world.time)
								temp_html += "<a href='?src=[REF(src)];task=injector;num=[i];text=ue'>Injector</a>"
							else
								temp_html += "<span class='linkOff'>Injector</span>"
						else
							temp_html += "<br>\tBlood Type: No Data"
							temp_html += "<br>\tUE: No Data"
						if(ui)
							temp_html += "<br>\tUI: [ui] "
							if(viable_occupant)
								temp_html += "<a href='?src=[REF(src)];task=transferbuffer;num=[i];text=ui'>Occupant</a>"
							else
								temp_html += "<span class='linkOff'>Occupant</span>"
							temp_html += "<a href='?src=[REF(src)];task=setdelayed;num=[i];delayaction=[SCANNER_ACTION_UI]'>Occupant:Delayed</a>"
							if(injectorready < world.time)
								temp_html += "<a href='?src=[REF(src)];task=injector;num=[i];text=ui'>Injector</a>"
							else
								temp_html += "<span class='linkOff'>Injector</span>"
						else
							temp_html += "<br>\tUI: No Data"
						if(ue && name && blood_type && ui)
							temp_html += "<br>\tUI+UE: [ui]/[ue] "
							if(viable_occupant)
								temp_html += "<a href='?src=[REF(src)];task=transferbuffer;num=[i];text=mixed'>Occupant</a>"
							else
								temp_html += "<span class='linkOff'>Occupant</span>"
							temp_html += "<a href='?src=[REF(src)];task=setdelayed;num=[i];delayaction=[SCANNER_ACTION_MIXED]'>Occupant:Delayed</a>"
							if(injectorready < world.time)
								temp_html += "<a href='?src=[REF(src)];task=injector;num=[i];text=mixed'>UI+UE Injector</a>"
							else
								temp_html += "<span class='linkOff'>UI+UE Injector</span>"
						if(viable_occupant)
							temp_html += "<br><a href='?src=[REF(src)];task=setbuffer;num=[i];'>Save to Buffer</a>"
						else
							temp_html += "<br><span class='linkOff'>Save to Buffer</span>"
						temp_html += "<a href='?src=[REF(src)];task=clearbuffer;num=[i];'>Clear Buffer</a>"
						if(diskette)
							temp_html += "<a href='?src=[REF(src)];task=loaddisk;num=[i];'>Load from Disk</a>"
						else
							temp_html += "<span class='linkOff'>Load from Disk</span>"
						if(diskette && !diskette.read_only)
							temp_html += "<a href='?src=[REF(src)];task=savedisk;num=[i];'>Save to Disk</a>"
						else
							temp_html += "<span class='linkOff'>Save to Disk</span>"
		if("disk")
			temp_html += status
			temp_html += buttons
			if(diskette)
				temp_html += "<h3>[diskette.name]</h3><br>"
				temp_html += "<a href='?src=[REF(src)];task=ejectdisk'>Eject Disk</a><br>"
				if(LAZYLEN(diskette.mutations))
					temp_html += "<table>"
					for(var/datum/mutation/human/A in diskette.mutations)
						temp_html += "<tr><td><span class='linkOff'>[A.name]</span></td>"
						temp_html += "<td><a href='?src=[REF(src)];task=deletediskmut;num=[diskette.mutations.Find(A)];'>Delete</a></td>"
						if(LAZYLEN(stored_mutations) < max_storage)
							temp_html += "<td><a href='?src=[REF(src)];task=importdiskmut;num=[diskette.mutations.Find(A)];'>Import</a></td>"
						else
							temp_html += "<td><td><span class='linkOff'>Import</span></td>"
						temp_html += "</tr>"
					temp_html += "</table>"
			else
				temp_html += "<br>Load diskette to start ----------"
		if("info")
			if(LAZYLEN(stored_mutations))
				if(LAZYLEN(stored_mutations) >= current_storage)
					var/datum/mutation/human/HM = stored_mutations[current_storage]
					if(HM)
						temp_html += display_sequence(HM.type, current_storage)
			else
				current_screen = "mainmenu"
		if("mutations")
			temp_html += status
			temp_html += buttons
			temp_html += "<h3>Mutation Storage:<br></h3>"
			temp_html += "<table>"
			for(var/datum/mutation/human/HM in stored_mutations)
				var/i = stored_mutations.Find(HM)
				temp_html += "<tr><td><a href='?src=[REF(src)];task=inspectstorage;num=[i]'>[HM.name]</a></td>"
				if(diskette)
					temp_html += "<td><a href='?src=[REF(src)];task=exportdiskmut;path=[HM.type]'>Export</a></td>"
				else
					temp_html += "<td><td><span class='linkOff'>Export</span></td>"
				temp_html += "<td><a href='?src=[REF(src)];task=deletemut;num=[i]'>Delete</a></td>"
				if(combine == HM.type)
					temp_html += "<td><span class='linkOff'>Combine</span></td></tr>"
				else
					temp_html += "<td><a href='?src=[REF(src)];task=combine;num=[i]'>Combine</a></td></tr>"
			temp_html += "</table><br>"
			temp_html += "<h3>Chromosome Storage:<br></h3>"
			temp_html += "<table>"
			for(var/i in 1 to stored_chromosomes.len)
				var/obj/item/chromosome/CM = stored_chromosomes[i]
				temp_html += "<td><a href='?src=[REF(src)];task=ejectchromosome;num=[i]'>[CM.name]</a></td><br>"
			temp_html += "</table>"
		if("advinjector")
			temp_html += status
			temp_html += buttons
			temp_html += "<div class='line'><div class='statusLabel'><b>Advanced Injectors:</b></div></div><br>"
			temp_html += "<div class='statusLine'><a href='?src=[REF(src)];task=add_advinjector;'>New Selection</a></div>"
			for(var/A in injector_selection)
				temp_html += "<div class='statusDisplay'><b>[A]</b>"
				var/list/true_selection = injector_selection[A]
				temp_html += "<br>"
				for(var/B in true_selection)
					var/datum/mutation/human/HM = B
					var/mutcolor
					switch(HM.quality)
						if(POSITIVE)
							mutcolor = "good"
						if(MINOR_NEGATIVE)
							mutcolor = "average"
						if(NEGATIVE)
							mutcolor = "bad"
					temp_html += "<div class='statusLine'><span class='[mutcolor]'>[HM.name] </span>"
					temp_html += "<a href='?src=[REF(src)];task=remove_from_advinjector;injector=[A];path=[HM.type];'>Remove</a></div>"
				if(injectorready < world.time)
					temp_html += "<div class='statusLine'> <a href='?src=[REF(src)];task=advinjector;injector=[A];'>Print Advanced Injector</a>"
				else
					temp_html += "<div class='statusLine'> <span class='linkOff'>Printer ready in [DisplayTimeText(injectorready - world.time, 1)]</span>"
				temp_html += "<a href='?src=[REF(src)];task=remove_advinjector;injector=[A];'>Remove Injector</a></div>"
				temp_html += "<br></div>"

		else
			temp_html += status
			temp_html += buttons
			temp_html += "<div class='line'><div class='statusLabel'>Genetic Sequence:</div><br>"
			if(viable_occupant)
				if(viable_occupant)
					for(var/A in get_mutation_list())
						temp_html += display_inactive_sequence(A)
					temp_html += "<br>"
				else
					temp_html += "----"
				if(viable_occupant && (current_mutation in get_mutation_list(viable_occupant)))
					temp_html += display_sequence(current_mutation)
				temp_html += "</div><br>"
			else
				temp_html += "----------"

	popup.set_content(temp_html.Join())
	popup.open()

/obj/machinery/computer/scan_consolenew/proc/display_inactive_sequence(mutation)
	var/temp_html = ""
	var/class = "unselected"
	var/mob/living/carbon/viable_occupant = get_viable_occupant()
	if(!viable_occupant)
		return

	var/location = viable_occupant.dna.mutation_index.Find(mutation) //We do this because we dont want people using sysexp or similair tools to just read the mutations.

	if(!location) //Do this only when needed, dont make a list with mutations for every iteration if you dont need to
		var/list/mutations = get_mutation_list(TRUE)
		if(mutation in mutations)
			location = mutations.Find(mutation)
	if(mutation == current_mutation)
		class = "selected"
	if(location > DNA_MUTATION_BLOCKS)
		temp_html += "<a class='clean' href='?src=[REF(src)];task=inspect;num=[location];'><img class='[class]' src='dna_extra.gif' width = '65'  alt='Extra Mutation'></a>"
	else if(mutation in stored_research.discovered_mutations)
		temp_html += "<a class='clean' href='?src=[REF(src)];task=inspect;num=[location];'><img class='[class]' src='dna_discovered.gif' width = '65'  alt='Discovered Mutation'></a>"
	else
		temp_html += "<a class='clean' clean href='?src=[REF(src)];task=inspect;num=[location];'><img class='[class]' src='dna_undiscovered.gif' width = '65' alt=Undiscovered Mutation'></a>"
	return temp_html

/obj/machinery/computer/scan_consolenew/proc/display_sequence(mutation, storage_slot) //Storage slot is for when viewing from the stored mutations
	var/temp_html = ""
	if(!mutation)
		temp_html += "ERR-"
		return
	var/mut_name = "Unknown gene"
	var/mut_desc = "No information available."
	var/alias
	var/discovered = FALSE
	var/active = FALSE
	var/scrambled = FALSE
	var/instability
	var/mob/living/carbon/viable_occupant = get_viable_occupant()
	var/datum/mutation/human/HM = get_valid_mutation(mutation)

	if(viable_occupant)
		var/datum/mutation/human/M = viable_occupant.dna.get_mutation(mutation)
		if(M)
			scrambled = M.scrambled
			active = TRUE
	var/datum/mutation/human/A = GET_INITIALIZED_MUTATION(mutation)
	alias = A.alias
	if(active && !scrambled)
		discover(mutation)
	if(stored_research && (mutation in stored_research.discovered_mutations))
		mut_name = A.name
		mut_desc = A.desc
		discovered = TRUE
		instability = A.instability
	var/extra
	if(viable_occupant && !(storage_slot || viable_occupant.dna.mutation_in_sequence(mutation)))
		extra = TRUE
	if(discovered && !scrambled)
		var/mutcolor
		switch(A.quality)
			if(POSITIVE)
				mutcolor = "good"
			if(MINOR_NEGATIVE)
				mutcolor = "average"
			if(NEGATIVE)
				mutcolor = "bad"
		if(HM)
			instability *= GET_MUTATION_STABILIZER(HM)
		temp_html += "<div class='statusDisplay'><div class='statusLine'><span class='[mutcolor]'><b>[mut_name]</b></span><small> ([alias])</small><br>"
		temp_html += "<div class='statusLine'>Instability : [round(instability)]</span><br>"
	else
		temp_html += "<div class='statusDisplay'><div class='statusLine'><b>[alias]</b><br>"
	temp_html += "<div class='statusLine'>[mut_desc]<br></div>"
	if(active && !storage_slot)
		if(HM?.can_chromosome && (HM in viable_occupant.dna.mutations))
			var/i = viable_occupant.dna.mutations.Find(HM)
			var/chromosome_name = "<a href='?src=[REF(src)];task=applychromosome;path=[mutation];num=[i];'>----</a>"
			if(HM.chromosome_name)
				chromosome_name = HM.chromosome_name
			temp_html += "<div class='statusLine'>Chromosome status: [chromosome_name]<br></div>"
	temp_html += "<div class='statusLine'>Sequence:<br><br></div>"
	if(!scrambled)
		for(var/block in 1 to A.blocks)
			var/whole_sequence = get_valid_gene_string(mutation)
			var/sequence = copytext_char(whole_sequence, 1+(block-1)*(DNA_SEQUENCE_LENGTH*2),(DNA_SEQUENCE_LENGTH*2*block+1))
			temp_html += "<div class='statusLine'><table class='statusDisplay'><tr>"
			for(var/i in 1 to DNA_SEQUENCE_LENGTH)
				var/num = 1+(i-1)*2
				var/genenum = num+(DNA_SEQUENCE_LENGTH*2*(block-1))
				temp_html += "<td><div class='statusLine'><span class='dnaBlockNumber'><a href='?src=[REF(src)];task=pulsegene;num=[genenum];alias=[alias];'>[sequence[num]]</span></a></div></td>"
			temp_html += "</tr><tr>"
			for(var/i in 1 to DNA_SEQUENCE_LENGTH)
				temp_html += "<td><div class='statusLine'>|</div></td>"
			temp_html += "</tr><tr>"
			for(var/i in 1 to DNA_SEQUENCE_LENGTH)
				var/num = i*2
				var/genenum = num+(DNA_SEQUENCE_LENGTH*2*(block-1))
				temp_html += "<td><div class='statusLine'><span class='dnaBlockNumber'><a href='?src=[REF(src)];task=pulsegene;num=[genenum];alias=[alias];'>[sequence[num]]</span></a></div></td>"
			temp_html += "</tr></table></div>"
		temp_html += "<br><br><br><br><br>"
	else
		temp_html = "<div class='statusLine'>Sequence unreadable due to unpredictable mutation.</div>"
	if((active || storage_slot) && (injectorready < world.time) && !scrambled)
		temp_html += "<a href='?src=[REF(src)];task=activator;path=[mutation];slot=[storage_slot];'>Print Activator</a>"
		temp_html += "<a href='?src=[REF(src)];task=mutator;path=[mutation];slot;=[storage_slot];'>Print Mutator</a>"
	else
		temp_html += "<span class='linkOff'>Print Activator</span>"
		temp_html += "<span class='linkOff'>Print Mutator</span>"
	temp_html += "<br><div class='statusLine'>"
	if(storage_slot)
		temp_html += "<a href='?src=[REF(src)];task=deletemut;num=[storage_slot];'>Delete</a>"
		if((LAZYLEN(stored_mutations) < max_storage) && diskette && !diskette.read_only)
			temp_html += "<a href='?src=[REF(src)];task=exportdiskmut;path=[mutation];'>Export</a>"
		else
			temp_html += "<span class='linkOff'>Export</span>"
		temp_html += "<a href='?src=[REF(src)];task=screen;text=mutations;'>Back</a>"
	else if(active && !scrambled)
		temp_html += "<a href='?src=[REF(src)];task=savemut;path=[mutation];'>Store</a>"
		temp_html += "<a href='?src=[REF(src)];task=expand_advinjector;path=[mutation];'>Adv. Injector</a>"
	if(extra || scrambled)
		temp_html += "<a href='?src=[REF(src)];task=nullify;'>Nullify</a>"
	else
		temp_html += "<span class='linkOff'>Nullify</span>"
	temp_html += "</div></div>"
	return temp_html

/obj/machinery/computer/scan_consolenew/Topic(href, href_list)
	if(..())
		return
	if(!isturf(usr.loc))
		return
	if(!((isturf(loc) && in_range(src, usr)) || issilicon(usr)))
		return
	if(current_screen == "working")
		return

	add_fingerprint(usr)
	usr.set_machine(src)

	var/mob/living/carbon/viable_occupant = get_viable_occupant()

	//Basic Tasks///////////////////////////////////////////
	var/num = round(text2num(href_list["num"]))
	var/last_change
	switch(href_list["task"])
		if("togglelock")
			if(connected)
				connected.locked = !connected.locked
		if("toggleopen")
			if(connected)
				connected.toggle_open(usr)
		if("setduration")
			if(!num)
				num = round(input(usr, "Choose pulse duration:", "Input an Integer", null) as num|null)
			if(num)
				radduration = WRAP(num, 1, RADIATION_DURATION_MAX+1)
		if("setstrength")
			if(!num)
				num = round(input(usr, "Choose pulse strength:", "Input an Integer", null) as num|null)
			if(num)
				radstrength = WRAP(num, 1, RADIATION_STRENGTH_MAX+1)
		if("screen")
			current_screen = href_list["text"]
		if("scramble")
			if(viable_occupant && (scrambleready < world.time))
				viable_occupant.dna.remove_all_mutations(list(MUT_NORMAL, MUT_EXTRA))
				viable_occupant.dna.generate_dna_blocks()
				scrambleready = world.time + SCRAMBLE_TIMEOUT
				to_chat(usr,"<span class'notice'>DNA scrambled.</span>")
				viable_occupant.radiation += RADIATION_STRENGTH_MULTIPLIER*50/(connected.damage_coeff ** 2)

		if("setbufferlabel")
			var/text = sanitize(input(usr, "Input a new label:", "Input a Text", null) as text|null)
			if(num && text)
				num = CLAMP(num, 1, NUMBER_OF_BUFFERS)
				var/list/buffer_slot = buffer[num]
				if(istype(buffer_slot))
					buffer_slot["label"] = text
		if("setbuffer")
			if(num && viable_occupant)
				num = CLAMP(num, 1, NUMBER_OF_BUFFERS)
				buffer[num] = list(
					"label"="Buffer[num]:[viable_occupant.real_name]",
					"UI"=viable_occupant.dna.uni_identity,
					"UE"=viable_occupant.dna.unique_enzymes,
					"name"=viable_occupant.real_name,
					"blood_type"=viable_occupant.dna.blood_type
					)
		if("clearbuffer")
			if(num)
				num = CLAMP(num, 1, NUMBER_OF_BUFFERS)
				var/list/buffer_slot = buffer[num]
				if(istype(buffer_slot))
					buffer_slot.Cut()
		if("transferbuffer")
			if(num && viable_occupant)
				switch(href_list["text"])                                                                            //Numbers are this high because other way upgrading laser is just not worth the hassle, and i cant think of anything better to inmrove
					if("ui")
						apply_buffer(SCANNER_ACTION_UI,num)
					if("ue")
						apply_buffer(SCANNER_ACTION_UE,num)
					if("mixed")
						apply_buffer(SCANNER_ACTION_MIXED,num)
		if("injector")
			if(num && injectorready < world.time)
				num = CLAMP(num, 1, NUMBER_OF_BUFFERS)
				var/list/buffer_slot = buffer[num]
				if(istype(buffer_slot))
					var/obj/item/dnainjector/timed/I
					switch(href_list["text"])
						if("ui")
							if(buffer_slot["UI"])
								I = new /obj/item/dnainjector/timed(loc)
								I.fields = list("UI"=buffer_slot["UI"])
								if(connected)
									I.damage_coeff = connected.damage_coeff
						if("ue")
							if(buffer_slot["name"] && buffer_slot["UE"] && buffer_slot["blood_type"])
								I = new /obj/item/dnainjector/timed(loc)
								I.fields = list("name"=buffer_slot["name"], "UE"=buffer_slot["UE"], "blood_type"=buffer_slot["blood_type"])
								if(connected)
									I.damage_coeff  = connected.damage_coeff
						if("mixed")
							if(buffer_slot["UI"] && buffer_slot["name"] && buffer_slot["UE"] && buffer_slot["blood_type"])
								I = new /obj/item/dnainjector/timed(loc)
								I.fields = list("UI"=buffer_slot["UI"],"name"=buffer_slot["name"], "UE"=buffer_slot["UE"], "blood_type"=buffer_slot["blood_type"])
								if(connected)
									I.damage_coeff = connected.damage_coeff
					if(I)
						injectorready = world.time + INJECTOR_TIMEOUT
		if("loaddisk")
			if(num && diskette && diskette.fields)
				num = CLAMP(num, 1, NUMBER_OF_BUFFERS)
				buffer[num] = diskette.fields.Copy()
		if("savedisk")
			if(num && diskette && !diskette.read_only)
				num = CLAMP(num, 1, NUMBER_OF_BUFFERS)
				var/list/buffer_slot = buffer[num]
				if(istype(buffer_slot))
					diskette.name = "data disk \[[buffer_slot["label"]]\]"
					diskette.fields = buffer_slot.Copy()
		if("ejectdisk")
			if(diskette)
				diskette.forceMove(drop_location())
				diskette = null
		if("setdelayed")
			if(num)
				delayed_action = list("action"=text2num(href_list["delayaction"]),"buffer"=num)
		if("pulseui")
			if(num && viable_occupant && connected)
				radduration = WRAP(radduration, 1, RADIATION_DURATION_MAX+1)
				radstrength = WRAP(radstrength, 1, RADIATION_STRENGTH_MAX+1)

				var/locked_state = connected.locked
				connected.locked = TRUE

				current_screen = "working"
				ui_interact(usr)

				sleep(radduration*10)
				current_screen = "ui"

				if(viable_occupant && connected && connected.occupant==viable_occupant)
					viable_occupant.radiation += (RADIATION_IRRADIATION_MULTIPLIER*radduration*radstrength)/(connected.damage_coeff ** 2) //Read comment in "transferbuffer" section above for explanation
					switch(href_list["task"])                                                                                             //Same thing as there but values are even lower, on best part they are about 0.0*, effectively no damage
						if("pulseui")
							var/len = length_char(viable_occupant.dna.uni_identity)
							num = WRAP(num, 1, len+1)
							num = randomize_radiation_accuracy(num, radduration + (connected.precision_coeff ** 2), len) //Each manipulator level above 1 makes randomization as accurate as selected time + manipulator lvl^2
                                                                                                                         //Value is this high for the same reason as with laser - not worth the hassle of upgrading if the bonus is low
							var/block = round((num-1)/DNA_BLOCK_SIZE)+1
							var/subblock = num - block*DNA_BLOCK_SIZE
							last_change = "UI #[block]-[subblock]; "

							var/hex = copytext_char(viable_occupant.dna.uni_identity, num, num+1)
							last_change += "[hex]"
							hex = scramble(hex, radstrength, radduration)
							last_change += "->[hex]"

							viable_occupant.dna.uni_identity = copytext_char(viable_occupant.dna.uni_identity, 1, num) + hex + copytext_char(viable_occupant.dna.uni_identity, num + 1)
							viable_occupant.updateappearance(mutations_overlay_update=1)
				else
					current_screen = "mainmenu"

				if(connected)
					connected.locked = locked_state
		if("inspect")
			if(viable_occupant)
				var/list/mutations = get_mutation_list(TRUE)
				if(current_mutation == mutations[num])
					current_mutation = null
				else
					current_mutation = mutations[num]

		if("inspectstorage")
			current_storage = num
			current_screen = "info"
		if("savemut")
			if(viable_occupant)
				var/succes
				if(LAZYLEN(stored_mutations) < max_storage)
					var/mutation = text2path(href_list["path"])
					if(ispath(mutation, /datum/mutation/human)) //sanity checks
						var/datum/mutation/human/HM = viable_occupant.dna.get_mutation(mutation)
						if(HM)
							var/datum/mutation/human/A = new HM.type()
							A.copy_mutation(HM)
							succes = TRUE
							stored_mutations += A
							to_chat(usr,"<span class='notice'>Mutation succesfully stored.</span>")
				if(!succes) //we can exactly return here
					to_chat(usr,"<span class='warning'>Mutation storage is full.</span>")
		if("deletemut")
			var/datum/mutation/human/HM = stored_mutations[num]
			if(HM)
				stored_mutations.Remove(HM)
				qdel(HM)
				current_screen = "mutations"
		if("activator")
			if(injectorready < world.time)
				var/mutation = text2path(href_list["path"])
				if(ispath(mutation, /datum/mutation/human))
					var/datum/mutation/human/HM = get_valid_mutation(mutation)
					if(HM)
						var/obj/item/dnainjector/activator/I = new /obj/item/dnainjector/activator(loc)
						I.add_mutations += new HM.type (copymut = HM)
						I.name = "[HM.name] activator"
						I.research = TRUE
						if(connected)
							I.damage_coeff = connected.damage_coeff*4
							injectorready = world.time + INJECTOR_TIMEOUT * (1 - 0.1 * connected.precision_coeff) //precision_coeff being the matter bin rating
						else
							injectorready = world.time + INJECTOR_TIMEOUT
		if("mutator")
			if(injectorready < world.time)
				var/mutation = text2path(href_list["path"])
				if(ispath(mutation, /datum/mutation/human))
					var/datum/mutation/human/HM = get_valid_mutation(mutation)
					if(HM)
						var/obj/item/dnainjector/activator/I = new /obj/item/dnainjector/activator(loc)
						I.add_mutations += new HM.type (copymut = HM)
						I.doitanyway = TRUE
						I.name = "[HM.name] injector"
						if(connected)
							I.damage_coeff = connected.damage_coeff
							injectorready = world.time + INJECTOR_TIMEOUT * 5 * (1 - 0.1 * connected.precision_coeff)
						else
							injectorready = world.time + INJECTOR_TIMEOUT * 5

		if("advinjector")
			var/selection = href_list["injector"]
			if(injectorready < world.time)
				if(injector_selection.Find(selection))
					var/list/true_selection = injector_selection[selection]
					if(LAZYLEN(injector_selection))
						var/obj/item/dnainjector/activator/I = new /obj/item/dnainjector/activator(loc)
						for(var/A in true_selection)
							var/datum/mutation/human/HM = A
							I.add_mutations += new HM.type (copymut = HM)
						I.doitanyway = TRUE
						I.name = "Advanced [selection] injector"
						if(connected)
							I.damage_coeff = connected.damage_coeff
							injectorready = world.time + INJECTOR_TIMEOUT * 8 * (1 - 0.1 * connected.precision_coeff)
						else
							injectorready = world.time + INJECTOR_TIMEOUT * 8

		if("nullify")
			if(viable_occupant)
				var/datum/mutation/human/A = viable_occupant.dna.get_mutation(current_mutation)
				if(A && (!viable_occupant.dna.mutation_in_sequence(current_mutation) || A.scrambled))
					viable_occupant.dna.remove_mutation(current_mutation)
					current_screen = "mainmenu"
					current_mutation = null
		if("pulsegene")
			if(current_screen != "info")
				var/path = GET_MUTATION_TYPE_FROM_ALIAS(href_list["alias"])
				if(viable_occupant && num && (path in viable_occupant.dna.mutation_index))
					var/list/genes = list("A","T","G","C","X")
					if(jokerready < world.time)
						genes += "JOKER"
					var/sequence = GET_GENE_STRING(path, viable_occupant.dna)
					var/original = sequence[num]
					var/new_gene = input("From [original] to-", "New block", original) as null|anything in genes
					if(!new_gene)
						new_gene = original
					if(viable_occupant == get_viable_occupant()) //No cheesing
						if((new_gene == "JOKER") && (jokerready < world.time))
							var/true_genes = GET_SEQUENCE(current_mutation)
							new_gene = true_genes[num]
							jokerready = world.time + JOKER_TIMEOUT - (JOKER_UPGRADE * (connected.precision_coeff-1))
						sequence = copytext_char(sequence, 1, num) + new_gene + copytext_char(sequence, num + 1)
						viable_occupant.dna.mutation_index[path] = sequence
					viable_occupant.radiation += RADIATION_STRENGTH_MULTIPLIER/connected.damage_coeff
					viable_occupant.domutcheck()
		if("exportdiskmut")
			if(diskette && !diskette.read_only)
				var/path = text2path(href_list["path"])
				if(ispath(path, /datum/mutation/human))
					var/datum/mutation/human/A = get_valid_mutation(path)
					if(A && diskette && (LAZYLEN(diskette.mutations) < diskette.max_mutations))
						var/datum/mutation/human/HM = new A.type()
						diskette.mutations += HM
						HM.copy_mutation(A)
						to_chat(usr, "<span class='notice'>Successfully wrote [A.name] to [diskette.name].</span>")
		if("deletediskmut")
			if(diskette && !diskette.read_only)
				if(num && (LAZYLEN(diskette.mutations) >= num))
					var/datum/mutation/human/A = diskette.mutations[num]
					diskette.mutations.Remove(A)
					qdel(A)
		if("importdiskmut")
			if(diskette && (LAZYLEN(diskette.mutations) >= num))
				if(LAZYLEN(stored_mutations) < max_storage)
					var/datum/mutation/human/A = diskette.mutations[num]
					var/datum/mutation/human/HM = new A.type()
					HM.copy_mutation(A)
					stored_mutations += HM
					to_chat(usr,"<span class='notice'>Successfully wrote [A.name] to storage.</span>")
		if("combine")
			if(num && (LAZYLEN(stored_mutations) >= num))
				if(LAZYLEN(stored_mutations) < max_storage)
					var/datum/mutation/human/A = stored_mutations[num]
					var/path = A.type
					if(combine)
						var/result_path = get_mixed_mutation(combine, path)
						if(result_path)
							stored_mutations += new result_path()
							to_chat(usr, "<span class='boldnotice'>Success! New mutation has been added to storage</span>")
							discover(result_path)
							combine = null
						else
							to_chat(usr, "<span class='warning'>Failed. No mutation could be created.</span>")
							combine = null
					else
						combine = path
						to_chat(usr,"<span class='notice'>Selected [A.name] for combining</span>")
				else
					to_chat(usr, "<span class='warning'>Not enough space to store potential mutation.</span>")
		if("ejectchromosome")
			if(LAZYLEN(stored_chromosomes) <= num)
				var/obj/item/chromosome/CM = stored_chromosomes[num]
				CM.forceMove(drop_location())
				adjust_item_drop_location(CM)
				stored_chromosomes -= CM
		if("applychromosome")
			if(viable_occupant && (LAZYLEN(viable_occupant.dna.mutations) <= num))
				var/datum/mutation/human/HM = viable_occupant.dna.mutations[num]
				var/list/chromosomes = list()
				for(var/obj/item/chromosome/CM in stored_chromosomes)
					if(CM.can_apply(HM))
						chromosomes += CM
				if(chromosomes.len)
					var/obj/item/chromosome/CM = input("Select a chromosome to apply", "Apply Chromosome") as null|anything in sortNames(chromosomes)
					if(CM)
						to_chat(usr, "<span class='notice'>You apply [CM] to [HM.name].</span>")
						stored_chromosomes -= CM
						CM.apply(HM)
		if("expand_advinjector")
			var/mutation = text2path(href_list["path"])
			var/datum/mutation/human/HM = get_valid_mutation(mutation)
			if(HM && LAZYLEN(injector_selection))
				var/which_injector = input(usr, "Select Adv. Injector", "Advanced Injectors") as null|anything in injector_selection
				if(injector_selection.Find(which_injector))
					var/list/true_selection = injector_selection[which_injector]
					var/total_instability
					for(var/B in true_selection)
						var/datum/mutation/human/mootacion = B
						total_instability += mootacion.instability
					total_instability += HM.instability
					if((total_instability > max_injector_instability) || (true_selection.len + 1) > max_injector_mutations)
						to_chat(usr, "<span class='warning'>Adding more mutations would make the advanced injector too unstable!</span>")
					else
						true_selection += HM //reminder that this works. because I keep forgetting this works
		if("remove_from_advinjector")
			var/mutation = text2path(href_list["path"])
			var/selection = href_list["injector"]
			if(injector_selection.Find(selection))
				var/list/true_selection = injector_selection[selection]
				for(var/B in true_selection)
					var/datum/mutation/human/HM = B
					if(HM.type == mutation)
						true_selection -= HM
					break

		if("remove_advinjector")
			var/selection = href_list["injector"]
			for(selection in injector_selection)
				if(selection == selection)
					injector_selection.Remove(selection)

		if("add_advinjector")
			if(LAZYLEN(injector_selection) < max_injector_selections)
				var/new_selection = input(usr, "Enter Adv. Injector name", "Advanced Injectors") as text|null
				if(new_selection && !(new_selection in injector_selection))
					injector_selection[new_selection] = list()



	ui_interact(usr,last_change)

/obj/machinery/computer/scan_consolenew/proc/scramble(input,rs,rd) //hexadecimal genetics. dont confuse with scramble button
	var/length = length(input)
	var/ran = gaussian(0, rs*RADIATION_STRENGTH_MULTIPLIER)
	if(ran == 0)
		ran = pick(-1,1)	//hacky, statistically should almost never happen. 0-chance makes people mad though
	else if(ran < 0)
		ran = round(ran)	//negative, so floor it
	else
		ran = -round(-ran)	//positive, so ceiling it
	return num2hex(WRAP(hex2num(input)+ran, 0, 16**length), length)

/obj/machinery/computer/scan_consolenew/proc/randomize_radiation_accuracy(position, radduration, number_of_blocks)
	var/val = round(gaussian(0, RADIATION_ACCURACY_MULTIPLIER/radduration) + position, 1)
	return WRAP(val, 1, number_of_blocks+1)

/obj/machinery/computer/scan_consolenew/proc/get_viable_occupant()
	var/mob/living/carbon/viable_occupant = null
	if(connected)
		viable_occupant = connected.occupant
		if(!istype(viable_occupant) || !viable_occupant.dna || HAS_TRAIT(viable_occupant, TRAIT_RADIMMUNE) || HAS_TRAIT(viable_occupant, TRAIT_BADDNA))
			viable_occupant = null
	return viable_occupant

/obj/machinery/computer/scan_consolenew/proc/apply_buffer(action,buffer_num)
	buffer_num = CLAMP(buffer_num, 1, NUMBER_OF_BUFFERS)
	var/list/buffer_slot = buffer[buffer_num]
	var/mob/living/carbon/viable_occupant = get_viable_occupant()
	if(istype(buffer_slot))
		viable_occupant.radiation += rand(100/(connected.damage_coeff ** 2),250/(connected.damage_coeff ** 2))
		//15 and 40 are just magic numbers that were here before so i didnt touch them, they are initial boundaries of damage
		//Each laser level reduces damage by lvl^2, so no effect on 1 lvl, 4 times less damage on 2 and 9 times less damage on 3
		//Numbers are this high because other way upgrading laser is just not worth the hassle, and i cant think of anything better to inmrove
		switch(action)
			if(SCANNER_ACTION_UI)
				if(buffer_slot["UI"])
					viable_occupant.dna.uni_identity = buffer_slot["UI"]
					viable_occupant.updateappearance(mutations_overlay_update=1)
			if(SCANNER_ACTION_UE)
				if(buffer_slot["name"] && buffer_slot["UE"] && buffer_slot["blood_type"])
					viable_occupant.real_name = buffer_slot["name"]
					viable_occupant.name = buffer_slot["name"]
					viable_occupant.dna.unique_enzymes = buffer_slot["UE"]
					viable_occupant.dna.blood_type = buffer_slot["blood_type"]
			if(SCANNER_ACTION_MIXED)
				if(buffer_slot["UI"])
					viable_occupant.dna.uni_identity = buffer_slot["UI"]
					viable_occupant.updateappearance(mutations_overlay_update=1)
				if(buffer_slot["name"] && buffer_slot["UE"] && buffer_slot["blood_type"])
					viable_occupant.real_name = buffer_slot["name"]
					viable_occupant.name = buffer_slot["name"]
					viable_occupant.dna.unique_enzymes = buffer_slot["UE"]
					viable_occupant.dna.blood_type = buffer_slot["blood_type"]

/obj/machinery/computer/scan_consolenew/proc/on_scanner_close()
	if(delayed_action && get_viable_occupant())
		to_chat(connected.occupant, "<span class='notice'>[src] activates!</span>")
		apply_buffer(delayed_action["action"],delayed_action["buffer"])
		delayed_action = null //or make it stick + reset button ?

/obj/machinery/computer/scan_consolenew/proc/get_valid_mutation(mutation)
	var/mob/living/carbon/C = get_viable_occupant()
	if(C)
		var/datum/mutation/human/HM = C.dna.get_mutation(mutation)
		if(HM)
			return HM
	for(var/datum/mutation/human/A in stored_mutations)
		if(A.type == mutation)
			return A


/obj/machinery/computer/scan_consolenew/proc/get_mutation_list(include_storage) //Returns a list of the mutation index types and any extra mutations
	var/mob/living/carbon/viable_occupant = get_viable_occupant()
	var/list/paths = list()
	if(viable_occupant)
		for(var/A in viable_occupant.dna.mutation_index)
			paths += A
		for(var/datum/mutation/human/A in viable_occupant.dna.mutations)
			if(A.class == MUT_EXTRA)
				paths += A.type
	if(include_storage)
		for(var/datum/mutation/human/A in stored_mutations)
			paths += A.type
	return paths

/obj/machinery/computer/scan_consolenew/proc/get_valid_gene_string(mutation)
	var/mob/living/carbon/C = get_viable_occupant()
	if(C && (mutation in C.dna.mutation_index))
		return GET_GENE_STRING(mutation, C.dna)
	else if(C && (LAZYLEN(C.dna.mutations)))
		for(var/datum/mutation/human/A in C.dna.mutations)
			if(A.type == mutation)
				return GET_SEQUENCE(mutation)
	for(var/datum/mutation/human/A in stored_mutations)
		if(A.type == mutation)
			return GET_SEQUENCE(mutation)

/obj/machinery/computer/scan_consolenew/proc/discover(mutation)
	if(stored_research && !(mutation in stored_research.discovered_mutations))
		stored_research.discovered_mutations += mutation
		return TRUE
/////////////////////////// DNA MACHINES
#undef INJECTOR_TIMEOUT
#undef NUMBER_OF_BUFFERS

#undef RADIATION_STRENGTH_MAX
#undef RADIATION_STRENGTH_MULTIPLIER

#undef RADIATION_DURATION_MAX
#undef RADIATION_ACCURACY_MULTIPLIER

#undef RADIATION_IRRADIATION_MULTIPLIER

#undef SCANNER_ACTION_SE
#undef SCANNER_ACTION_UI
#undef SCANNER_ACTION_UE
#undef SCANNER_ACTION_MIXED

//#undef BAD_MUTATION_DIFFICULTY
//#undef GOOD_MUTATION_DIFFICULTY
//#undef OP_MUTATION_DIFFICULTY
