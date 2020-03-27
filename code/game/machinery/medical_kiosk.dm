//The Medical Kiosk is designed to act as a low access alernative to  a medical analyzer, and doesn't require breaking into medical. Self Diagnose at your heart's content!
//For a fee that is. Comes in 4 flavors of medical scan.


/obj/machinery/medical_kiosk
	name = "medical kiosk"
	desc = "A freestanding medical kiosk, which can provide a wide range of medical analysis for diagnosis."
	icon = 'icons/obj/machines/medical_kiosk.dmi'
	icon_state = "kiosk"
	layer = ABOVE_MOB_LAYER
	density = TRUE
	circuit = /obj/item/circuitboard/machine/medical_kiosk
	payment_department = ACCOUNT_MED
	var/obj/item/scanner_wand
	var/default_price = 15          //I'm defaulting to a low price on this, but in the future I wouldn't have an issue making it more or less expensive.
	var/active_price = 15           //Change by using a multitool on the board.
	var/pandemonium = FALSE			//AKA: Emag mode.

	var/scan_active_1 = FALSE       //Shows if the machine is being used for a general scan.
	var/scan_active_2 = FALSE 		//as above, symptom scan
	var/scan_active_3 = FALSE    	//as above, radiological scan
	var/scan_active_4 = FALSE		//as above, chemical/hallucinations.
	var/paying_customer = FALSE		//Ticked yes if passing inuse()

	var/datum/bank_account/account  //payer's account.
	var/mob/living/carbon/human/H   //The person using the console in each instance. Used for paying for the kiosk.
	var/mob/living/carbon/human/altPatient   //If scanning someone else, this will be the target.
	var/obj/item/card/id/C          //the account of the person using the console.

/obj/machinery/medical_kiosk/Initialize() //loaded subtype for mapping use
	. = ..()
	scanner_wand = new/obj/item/scanner_wand(src)

/obj/machinery/medical_kiosk/proc/inuse()  //Verifies that the user can use the interface, followed by showing medical information.
	if (pandemonium == TRUE)
		active_price += (rand(10,30)) //The wheel of capitalism says health care ain't cheap.
	if(!istype(C))
		say("No ID card detected.") // No unidentified crew.
		return
	if(C.registered_account)
		account = C.registered_account
	else
		say("No account detected.")  //No homeless crew.
		return
	if(account?.account_job?.paycheck_department == payment_department)
		use_power(20)
		paying_customer = TRUE
		say("Hello, esteemed medical staff!")
		RefreshParts()
		return
	if(!account.has_money(active_price))
		say("You do not possess the funds to purchase this.")  //No jobless crew, either.
		return
	else
		account.adjust_money(-active_price)
		var/datum/bank_account/D = SSeconomy.get_dep_account(ACCOUNT_MED)
		if(D)
			D.adjust_money(active_price)
		use_power(20)
		paying_customer = TRUE
	icon_state = "kiosk_active"
	say("Thank you for your patronage!")
	RefreshParts()
	return

/obj/machinery/medical_kiosk/proc/clearScans() //Called it enough times to be it's own proc
	scan_active_1 = FALSE
	scan_active_2 = FALSE
	scan_active_3 = FALSE
	scan_active_4 = FALSE
	return

/obj/machinery/medical_kiosk/update_icon_state()
	if(is_operational())
		icon_state = "kiosk_off"
	else
		icon_state = "kiosk"

/obj/machinery/medical_kiosk/wrench_act(mob/living/user, obj/item/I) //Allows for wrenching/unwrenching the machine.
	..()
	default_unfasten_wrench(user, I, time = 10)
	return TRUE

/obj/machinery/medical_kiosk/RefreshParts()
	var/obj/item/circuitboard/machine/medical_kiosk/board = circuit
	if(board)
		active_price = board.custom_cost
	return

/obj/machinery/medical_kiosk/attackby(obj/item/O, mob/user, params)
	if(default_deconstruction_screwdriver(user, "kiosk_open", "kiosk", O))
		return
	else if(default_deconstruction_crowbar(O))
		return

	if(istype(O, /obj/item/scanner_wand))
		var/obj/item/scanner_wand/W = O
		if(scanner_wand)
			to_chat(user, "<span class='warning'>There's already a scanner wand in [src]!</span>")
			return
		if(HAS_TRAIT(O, TRAIT_NODROP) || !user.transferItemToLoc(O, src))
			to_chat(user, "<span class='warning'>[O] is stuck to your hand!</span>")
			return
		user.visible_message("<span class='notice'>[user] snaps [O] onto [src]!</span>", \
		"<span class='notice'>You press [O] into the side of [src], clicking into place.</span>")
		 //This will be the scanner returning scanner_wand's selected_target variable and assigning it to the altPatient var
		if(W.selected_target)
			if(!(altPatient == W.return_patient()))
				clearScans()
			altPatient = W.return_patient()
			user.visible_message("<span class='notice'>[W.return_patient()] has been set as the current patient.</span>")
			W.selected_target = null
		playsound(src, 'sound/machines/click.ogg', 50, TRUE)
		scanner_wand = O
		return
	return ..()

/obj/machinery/medical_kiosk/AltClick(mob/living/carbon/user)
	if(!istype(user) || !user.canUseTopic(src, BE_CLOSE))
		return
	if(!scanner_wand)
		to_chat(user, "<span class='warning'>The scanner wand is currently removed from the machine.</span>")
		return
	if(!user.put_in_hands(scanner_wand))
		to_chat(user, "<span class='warning'>The scanner wand falls to the floor.</span>")
		scanner_wand = null
		return
	user.visible_message("<span class='notice'>[user] unhooks the [scanner_wand] from [src].</span>", \
	"<span class='notice'>You detach the [scanner_wand] from [src].</span>")
	playsound(src, 'sound/machines/click.ogg', 60, TRUE)
	scanner_wand = null

/obj/machinery/medical_kiosk/Destroy()
	qdel(scanner_wand)
	return ..()

/obj/machinery/medical_kiosk/emag_act(mob/user)
	..()
	if(obj_flags & EMAGGED)
		return
	if(user)
		user.visible_message("<span class='warning'>[user] waves a suspicious card by the [src]'s biometric scanner!</span>",
	"<span class='notice'>You overload the sensory electronics, the diagnostic readouts start jittering across the screen..</span>")
	obj_flags |= EMAGGED
	var/obj/item/circuitboard/computer/cargo/board = circuit
	board.obj_flags |= EMAGGED //Mirrors emag status onto the board as well.
	pandemonium = TRUE

/obj/machinery/medical_kiosk/examine(mob/user)
	. = ..()
	if(scanner_wand == null)
		. += "<span class='notice'>\The [src] is missing its scanner.</span>"
	else
		. += "<span class='notice'>\The [src] has its scanner clipped to the side. Alt-Click to remove.</span>"

/obj/machinery/medical_kiosk/ui_interact(mob/user, ui_key = "main", datum/tgui/ui = null, force_open = 0, datum/tgui/master_ui = null, datum/ui_state/state = GLOB.default_state)
	var/patient_distance = 0
	if(!ishuman(user))
		to_chat(user, "<span class='warning'>[src] is unable to interface with non-humanoids!</span>")
		if (ui)
			ui.close()
		return
	patient_distance = get_dist(src.loc,altPatient)
	if(altPatient == null)
		say("Scanner reset.")
		altPatient = user
	else if(patient_distance>5)
		altPatient = null
		say("Patient out of range. Resetting biometrics.")
		clearScans()
		return

	ui = SStgui.try_update_ui(user, src, ui_key, ui, force_open)
	if(!ui)
		ui = new(user, src, ui_key, "medical_kiosk", name, 625, 550, master_ui, state)
		ui.open()
		icon_state = "kiosk_off"
		RefreshParts()
		H = user
		C = H.get_idcard(TRUE)

/obj/machinery/medical_kiosk/ui_data(mob/living/carbon/human/user)
	var/list/data = list()
	var/patient_name = altPatient.name
	var/patient_status = "Alive."
	var/max_health = altPatient.maxHealth
	var/total_health = altPatient.health
	var/brute_loss = altPatient.getBruteLoss()
	var/fire_loss = altPatient.getFireLoss()
	var/tox_loss = altPatient.getToxLoss()
	var/oxy_loss = altPatient.getOxyLoss()
	var/chaos_modifier = 0

	var/sickness = "Patient does not show signs of disease."
	var/sickness_data = "Not Applicable."

	var/bleed_status = "Patient is not currently bleeding."
	var/blood_status = " Patient either has no blood, or does not require it to function."
	var/blood_percent =  round((altPatient.blood_volume / BLOOD_VOLUME_NORMAL)*100)
	var/blood_type = altPatient.dna.blood_type
	var/blood_warning = " "

	for(var/thing in altPatient.diseases) //Disease Information
		var/datum/disease/D = thing
		if(!(D.visibility_flags & HIDDEN_SCANNER))
			sickness = "Warning: Patient is harboring some form of viral disease. Seek further medical attention."
			sickness_data = "\nName: [D.name].\nType: [D.spread_text].\nStage: [D.stage]/[D.max_stages].\nPossible Cure: [D.cure_text]"

	if(altPatient.has_dna()) //Blood levels Information
		if(altPatient.bleed_rate)
			bleed_status = "Patient is currently bleeding!"
		if(blood_percent <= 80)
			blood_warning = " Patient has low blood levels. Seek a large meal, or iron supplements."
		if(blood_percent <= 60)
			blood_warning = " Patient has DANGEROUSLY low blood levels. Seek a blood transfusion, iron supplements, or saline glucose immedietly. Ignoring treatment may lead to death!"
		blood_status = "Patient blood levels are currently reading [blood_percent]%. Patient has [ blood_type] type blood. [blood_warning]"

	var/rad_value = altPatient.radiation
	var/rad_status = "Target within normal-low radiation levels."
	var/trauma_status = "Patient is free of unique brain trauma."
	var/clone_loss = altPatient.getCloneLoss()
	var/brain_loss = altPatient.getOrganLoss(ORGAN_SLOT_BRAIN)
	var/brain_status = "Brain patterns normal."
	if(LAZYLEN(user.get_traumas()))
		var/list/trauma_text = list()
		for(var/datum/brain_trauma/B in altPatient.get_traumas())
			var/trauma_desc = ""
			switch(B.resilience)
				if(TRAUMA_RESILIENCE_SURGERY)
					trauma_desc += "severe "
				if(TRAUMA_RESILIENCE_LOBOTOMY)
					trauma_desc += "deep-rooted "
				if(TRAUMA_RESILIENCE_MAGIC, TRAUMA_RESILIENCE_ABSOLUTE)
					trauma_desc += "permanent "
			trauma_desc += B.scan_desc
			trauma_text += trauma_desc
		trauma_status = "Cerebral traumas detected: patient appears to be suffering from [english_list(trauma_text)]."

	var/chem_status = FALSE
	var/chemical_list= list()
	var/overdose_status = FALSE
	var/overdose_list = list()
	var/addict_status = FALSE
	var/addict_list = list()
	var/hallucination_status = "Patient is not hallucinating."

	for(var/datum/reagent/R in altPatient.reagents.reagent_list)
		if(R.overdosed)
			overdose_status = TRUE

	if(altPatient.reagents.reagent_list.len)	//Chemical Analysis details.
		chem_status = TRUE
		for(var/datum/reagent/R in altPatient.reagents.reagent_list)
			chemical_list += list(list("name" = R.name, "volume" = round(R.volume, 0.01)))
			if(R.overdosed == 1)
				overdose_list += list(list("name" = R.name))
	else
		chemical_list = "Patient contains no reagents"

	if(altPatient.reagents.addiction_list.len)
		addict_status = TRUE
		for(var/datum/reagent/R in altPatient.reagents.addiction_list)
			addict_list += list(list("name" = R.name))
	if (altPatient.hallucinating())
		hallucination_status = "Subject appears to be hallucinating. Suggested treatments: bedrest, mannitol or psicodine."

	if(altPatient.stat == DEAD || HAS_TRAIT(altPatient, TRAIT_FAKEDEATH) || ((brute_loss+fire_loss+tox_loss+oxy_loss+clone_loss) >= 200))  //Patient status checks.
		patient_status = "Dead."
	if((brute_loss+fire_loss+tox_loss+oxy_loss+clone_loss) >= 80)
		patient_status = "Gravely Injured"
	else if((brute_loss+fire_loss+tox_loss+oxy_loss+clone_loss) >= 40)
		patient_status = "Injured"
	else if((brute_loss+fire_loss+tox_loss+oxy_loss+clone_loss) >= 20)
		patient_status = "Lightly Injured"
	if(pandemonium || user.hallucinating())
		patient_status = pick("The only kiosk is kiosk, but is the only patient, patient?", "Breathing manually.","Constact NTOS site admin.","97% carbon, 3% natural flavoring","The ebb and flow wears us all in time.","It's Lupus. You have Lupus.","Undergoing monkey disease.")

	if((brain_loss) >= 100)   //Brain status checks.
		brain_status = "Grave brain damage detected."
	else if((brain_loss) >= 50)
		brain_status = "Severe brain damage detected."
	else if((brain_loss) >= 20)
		brain_status = "Brain damage detected."
	else if((brain_loss) >= 1)
		brain_status = "Mild brain damage detected."  //You may have a miiiild case of severe brain damage.

	if(altPatient.radiation >=1000)  //
		rad_status = "Patient is suffering from extreme radiation poisoning. Suggested treatment: Isolation of patient, followed by repeated dosages of Pentetic Acid."
	else if(altPatient.radiation >= 500)
		rad_status = "Patient is suffering from alarming radiation poisoning. Suggested treatment: Heavy use of showers and decontamination of clothing. Take Pentetic Acid or Potassium Iodine."
	else if(altPatient.radiation >= 100)
		rad_status = "Patient has moderate radioactive signatures. Keep under showers until symptoms subside."

	if(pandemonium == TRUE)
		chaos_modifier = 1
	else if (user.hallucinating())
		chaos_modifier = 0.3


	data["kiosk_cost"] = active_price + (chaos_modifier * (rand(1,25)))
	data["patient_name"] = patient_name
	data["patient_health"] = round(((total_health - (chaos_modifier * (rand(1,50)))) / max_health) * 100, 0.001)
	data["brute_health"] = round(brute_loss+(chaos_modifier * (rand(1,30))),0.001)		//To break this down for easy reading, all health values are rounded to the .001 place
	data["burn_health"] = round(fire_loss+(chaos_modifier * (rand(1,30))),0.001)		//then a random number is added, which is multiplied by chaos modifier.
	data["toxin_health"] = round(tox_loss+(chaos_modifier * (rand(1,30))),0.001)		//That allows for a weaker version of the affect to be applied while hallucinating as opposed to emagged.
	data["suffocation_health"] = round(oxy_loss+(chaos_modifier * (rand(1,30))),0.001)	//It's not the cleanest but it does make for a colorful window.
	data["clone_health"] = round(clone_loss+(chaos_modifier * (rand(1,30))),0.001)
	data["brain_health"] = brain_status
	data["brain_damage"] = brain_loss+(chaos_modifier * (rand(1,30)))
	data["patient_status"] = patient_status
	data["rad_value"] = rad_value+(chaos_modifier * (rand(1,500)))
	data["rad_status"] = rad_status
	data["trauma_status"] = trauma_status
	data["patient_illness"] = sickness
	data["illness_info"] = sickness_data
	data["bleed_status"] = bleed_status
	data["blood_levels"] = blood_percent - (chaos_modifier * (rand(1,35)))
	data["blood_status"] = blood_status
	data["are_chems_present"] = chem_status ? TRUE : FALSE
	data["chemical_list"] = chemical_list
	data["are_overdoses_present"] = overdose_status ? TRUE : FALSE
	data["overdose_status"] = overdose_list
	data["are_addictions_present"] = addict_status ? TRUE : FALSE
	data["addiction_status"] = addict_list
	data["hallucinating_status"] = hallucination_status

	data["active_status_1"] = scan_active_1 ? FALSE : TRUE //General Scan Check
	data["active_status_2"] = scan_active_2 ? FALSE : TRUE	//Symptom Scan Check
	data["active_status_3"] = scan_active_3 ? FALSE : TRUE	//Radio-Neuro Scan Check
	data["active_status_4"] = scan_active_4 ? FALSE : TRUE	//Radio-Neuro Scan Check
	return data

/obj/machinery/medical_kiosk/ui_act(action,active)
	if(..())
		return
	switch(action)
		if("beginScan_1")
			inuse()
			if(paying_customer == TRUE)
				scan_active_1 = TRUE
				paying_customer = FALSE
		if("beginScan_2")
			inuse()
			if(paying_customer == TRUE)
				scan_active_2 = TRUE
				paying_customer = FALSE
		if("beginScan_3")
			inuse()
			if(paying_customer == TRUE)
				scan_active_3 = TRUE
				paying_customer = FALSE
		if("beginScan_4")
			inuse()
			if(paying_customer == TRUE)
				scan_active_4 = TRUE
				paying_customer = FALSE
		if("clearTarget")
			altPatient = null
			clearScans()
			. = TRUE
