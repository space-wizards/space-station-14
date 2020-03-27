// TODO:
//	- Potentially roll HUDs and Records into one
//	- Shock collar/lock system for prisoner pAIs?
//  - Put cable in user's hand instead of on the ground
//  - Camera jack


/mob/living/silicon/pai/var/list/available_software = list(
															//Nightvision
															//T-Ray
															//radiation eyes
															//chem goggs
															//mesons
															"crew manifest" = 5,
															"digital messenger" = 5,
															"atmosphere sensor" = 5,
															"photography module" = 5,
															"remote signaller" = 10,
															"medical records" = 10,
															"security records" = 10,
															"camera zoom" = 10,
															"host scan" = 10,
															//"camera jack" = 10,
															//"heartbeat sensor" = 10,
															//"projection array" = 15,
															"medical HUD" = 20,
															"security HUD" = 20,
															"loudness booster" = 20,
															"newscaster" = 20,
															"door jack" = 25,
															"encryption keys" = 25,
															"universal translator" = 35
															)

/mob/living/silicon/pai/proc/paiInterface()
	var/dat = ""
	var/left_part = ""
	var/right_part = softwareMenu()
	set_machine(src)

	if(temp)
		left_part = temp
	else if(stat == DEAD)						// Show some flavor text if the pAI is dead
		left_part = "<b><font color=red>ÈRrÖR Ða†Ä ÇÖRrÚþ†Ìoñ</font></b>"
		right_part = "<pre>Program index hash not found</pre>"

	else
		switch(screen)							// Determine which interface to show here
			if("main")
				left_part = ""
			if("directives")
				left_part = directives()
			if("pdamessage")
				left_part = pdamessage()
			if("buy")
				left_part = downloadSoftware()
			if("manifest")
				left_part = softwareManifest()
			if("medicalrecord")
				left_part = softwareMedicalRecord()
			if("securityrecord")
				left_part = softwareSecurityRecord()
			if("encryptionkeys")
				left_part = softwareEncryptionKeys()
			if("translator")
				left_part = softwareTranslator()
			if("atmosensor")
				left_part = softwareAtmo()
			if("securityhud")
				left_part = facialRecognition()
			if("medicalhud")
				left_part = medicalAnalysis()
			if("doorjack")
				left_part = softwareDoor()
			if("camerajack")
				left_part = softwareCamera()
			if("signaller")
				left_part = softwareSignal()
			if("loudness")
				left_part = softwareLoudness()
			if("hostscan")
				left_part = softwareHostScan()


	//usr << browse_rsc('windowbak.png')		// This has been moved to the mob's Login() proc


												// Declaring a doctype is necessary to enable BYOND's crappy browser's more advanced CSS functionality
	dat = {"<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\" \"http://www.w3.org/TR/html4/loose.dtd\">
			<html>
			<head>
				<style type=\"text/css\">
					body { background-image:url('html/paigrid.png'); }

					#header { text-align:center; color:white; font-size: 30px; height: 35px; width: 100%; letter-spacing: 2px; z-index: 5}
					#content {position: relative; left: 10px; height: 400px; width: 100%; z-index: 0}

					#leftmenu {color: #AAAAAA; background-color:#333333; width: 400px; height: auto; min-height: 340px; position: absolute; z-index: 0}
					#leftmenu a:link { color: #CCCCCC; }
					#leftmenu a:hover { color: #CC3333; }
					#leftmenu a:visited { color: #CCCCCC; }
					#leftmenu a:active { color: #000000; }

					#rightmenu {color: #CCCCCC; background-color:#555555; width: 200px ; height: auto; min-height: 340px; right: 10px; position: absolute; z-index: 1}
					#rightmenu a:link { color: #CCCCCC; }
					#rightmenu a:hover { color: #CC3333; }
					#rightmenu a:visited { color: #CCCCCC; }
					#rightmenu a:active { color: #000000; }

				</style>
				<script language='javascript' type='text/javascript'>
				[js_byjax]
				</script>
			</head>
			<body scroll=yes>
				<div id=\"header\">
					pAI OS
				</div>
				<div id=\"content\">
					<div id=\"leftmenu\">[left_part]</div>
					<div id=\"rightmenu\">[right_part]</div>
				</div>
			</body>
			</html>"} //"
	src << browse(dat, "window=pai;size=640x480;border=0;can_close=1;can_resize=1;can_minimize=1;titlebar=1")
	onclose(src, "pai")
	temp = null

/mob/living/silicon/pai/Topic(href, href_list)
	if(..())
		return
	var/soft = href_list["software"]
	var/sub = href_list["sub"]
	if(soft)
		screen = soft
		if(sub)
			subscreen = text2num(sub)
		switch(soft)
			if("buy") // Purchasing new software
				if(subscreen == 1)
					var/target = href_list["buy"]
					if(available_software.Find(target) && !software.Find(target))
						var/cost = available_software[target]
						if(ram >= cost)
							software.Add(target)
							ram -= cost
							var/datum/hud/pai/pAIhud = hud_used
							pAIhud?.update_software_buttons()
						else
							temp = "Insufficient RAM available."
					else
						temp = "Trunk <TT> \"[target]\"</TT> not found."


			if("radio") // Configuring onboard radio
				radio.attack_self(src)

			if("image") // Set pAI card display face
				var/newImage = input("Select your new display image.", "Display Image", "Happy") in sortList(list("Happy", "Cat", "Extremely Happy", "Face", "Laugh", "Off", "Sad", "Angry", "What", "Sunglasses"))
				var/pID = 1

				switch(newImage)
					if("Happy")
						pID = 1
					if("Cat")
						pID = 2
					if("Extremely Happy")
						pID = 3
					if("Face")
						pID = 4
					if("Laugh")
						pID = 5
					if("Off")
						pID = 6
					if("Sad")
						pID = 7
					if("Angry")
						pID = 8
					if("What")
						pID = 9
					if("Null")
						pID = 10
					if("Sunglasses")
						pID = 11
				card.setEmotion(pID)

			if("news")
				newscaster.ui_interact(src)

			if("camzoom")
				aicamera.adjust_zoom(usr)

			if("signaller")
				if(href_list["send"])
					signaler.send_activation()
					audible_message("[icon2html(src, hearers(src))] *beep* *beep* *beep*")
					playsound(src, 'sound/machines/triple_beep.ogg', ASSEMBLY_BEEP_VOLUME, TRUE)

				if(href_list["freq"])
					var/new_frequency = (signaler.frequency + text2num(href_list["freq"]))
					if(new_frequency < MIN_FREE_FREQ || new_frequency > MAX_FREE_FREQ)
						new_frequency = sanitize_frequency(new_frequency)
					signaler.set_frequency(new_frequency)

				if(href_list["code"])
					signaler.code += text2num(href_list["code"])
					signaler.code = round(signaler.code)
					signaler.code = min(100, signaler.code)
					signaler.code = max(1, signaler.code)

			if("directive")
				if(href_list["getdna"])
					if(iscarbon(card.loc))
						CheckDNA(card.loc, src) //you should only be able to check when directly in hand, muh immersions?
					else
						to_chat(src, "<span class='warning'>You are not being carried by anyone!</span>")
						return 0 // FALSE ? If you return here you won't call paiinterface() below

			if("pdamessage")
				if(!isnull(aiPDA))
					if(href_list["toggler"])
						aiPDA.toff = !aiPDA.toff
					else if(href_list["ringer"])
						aiPDA.silent = !aiPDA.silent
					else if(href_list["target"])
						if(silent)
							return alert("Communications circuits remain uninitialized.")
						var/target = locate(href_list["target"]) in GLOB.PDAs
						aiPDA.create_message(src, target)

			if("medicalrecord") // Accessing medical records
				if(subscreen == 1)
					medicalActive1 = find_record("id", href_list["med_rec"], GLOB.data_core.general)
					if(medicalActive1)
						medicalActive2 = find_record("id", href_list["med_rec"], GLOB.data_core.medical)
					if(!medicalActive2)
						medicalActive1 = null
						temp = "Unable to locate requested security record. Record may have been deleted, or never have existed."

			if("securityrecord")
				if(subscreen == 1)
					securityActive1 = find_record("id", href_list["sec_rec"], GLOB.data_core.general)
					if(securityActive1)
						securityActive2 = find_record("id", href_list["sec_rec"], GLOB.data_core.security)
					if(!securityActive2)
						securityActive1 = null
						temp = "Unable to locate requested security record. Record may have been deleted, or never have existed."

			if("securityhud")
				if(href_list["toggle"])
					secHUD = !secHUD
					if(secHUD)
						var/datum/atom_hud/sec = GLOB.huds[sec_hud]
						sec.add_hud_to(src)
					else
						var/datum/atom_hud/sec = GLOB.huds[sec_hud]
						sec.remove_hud_from(src)

			if("medicalhud")
				if(href_list["toggle"])
					medHUD = !medHUD
					if(medHUD)
						var/datum/atom_hud/med = GLOB.huds[med_hud]
						med.add_hud_to(src)
					else
						var/datum/atom_hud/med = GLOB.huds[med_hud]
						med.remove_hud_from(src)

			if("hostscan")
				if(href_list["toggle"])
					var/mob/living/silicon/pai/pAI = usr
					pAI.hostscan.attack_self(usr)
				if(href_list["toggle2"])
					var/mob/living/silicon/pai/pAI = usr
					pAI.hostscan.toggle_mode()

			if("encryptionkeys")
				if(href_list["toggle"])
					encryptmod = TRUE

			if("translator")
				if(href_list["toggle"])	//This is permanent.
					grant_all_languages(TRUE, TRUE, TRUE, LANGUAGE_SOFTWARE)

			if("doorjack")
				if(href_list["jack"])
					if(cable && cable.machine)
						hackdoor = cable.machine
						hackloop()
				if(href_list["cancel"])
					hackdoor = null
				if(href_list["cable"])
					var/turf/T = get_turf(loc)
					cable = new /obj/item/pai_cable(T)
					T.visible_message("<span class='warning'>A port on [src] opens to reveal [cable], which promptly falls to the floor.</span>", "<span class='hear'>You hear the soft click of something light and hard falling to the ground.</span>")

			if("loudness")
				if(subscreen == 1) // Open Instrument
					internal_instrument.interact(src)
				if(subscreen == 2) // Change Instrument type
					internal_instrument.selectInstrument()

		paiInterface()

// MENUS

/mob/living/silicon/pai/proc/softwareMenu()			// Populate the right menu
	var/dat = ""

	dat += "<A href='byond://?src=[REF(src)];software=refresh'>Refresh</A><br>"
	// Built-in
	dat += "<A href='byond://?src=[REF(src)];software=directives'>Directives</A><br>"
	dat += "<A href='byond://?src=[REF(src)];software=radio;sub=0'>Radio Configuration</A><br>"
	dat += "<A href='byond://?src=[REF(src)];software=image'>Screen Display</A><br>"
	//dat += "Text Messaging <br>"
	dat += "<br>"

	// Basic
	dat += "<b>Basic</b> <br>"
	for(var/s in software)
		if(s == "digital messenger")
			dat += "<a href='byond://?src=[REF(src)];software=pdamessage;sub=0'>Digital Messenger</a> <br>"
		if(s == "crew manifest")
			dat += "<a href='byond://?src=[REF(src)];software=manifest;sub=0'>Crew Manifest</a> <br>"
		if(s == "host scan")
			dat += "<a href='byond://?src=[REF(src)];software=hostscan;sub=0'>Host Health Scan</a> <br>"
		if(s == "medical records")
			dat += "<a href='byond://?src=[REF(src)];software=medicalrecord;sub=0'>Medical Records</a> <br>"
		if(s == "security records")
			dat += "<a href='byond://?src=[REF(src)];software=securityrecord;sub=0'>Security Records</a> <br>"
		if(s == "camera")
			dat += "<a href='byond://?src=[REF(src)];software=[s]'>Camera Jack</a> <br>"
		if(s == "remote signaller")
			dat += "<a href='byond://?src=[REF(src)];software=signaller;sub=0'>Remote Signaller</a> <br>"
		if(s == "loudness booster")
			dat += "<a href='byond://?src=[REF(src)];software=loudness;sub=0'>Loudness Booster</a> <br>"
	dat += "<br>"

	// Advanced
	dat += "<b>Advanced</b> <br>"
	for(var/s in software)
		if(s == "camera zoom")
			dat += "<a href='byond://?src=[REF(src)];software=camzoom;sub=0'>Adjust Camera Zoom</a> <br>"
		if(s == "atmosphere sensor")
			dat += "<a href='byond://?src=[REF(src)];software=atmosensor;sub=0'>Atmospheric Sensor</a> <br>"
		if(s == "heartbeat sensor")
			dat += "<a href='byond://?src=[REF(src)];software=[s]'>Heartbeat Sensor</a> <br>"
		if(s == "security HUD")
			dat += "<a href='byond://?src=[REF(src)];software=securityhud;sub=0'>Facial Recognition Suite</a>[(secHUD) ? "<font color=#55FF55> On</font>" : "<font color=#FF5555> Off</font>"] <br>"
		if(s == "medical HUD")
			dat += "<a href='byond://?src=[REF(src)];software=medicalhud;sub=0'>Medical Analysis Suite</a>[(medHUD) ? "<font color=#55FF55> On</font>" : "<font color=#FF5555> Off</font>"] <br>"
		if(s == "encryption keys")
			dat += "<a href='byond://?src=[REF(src)];software=encryptionkeys;sub=0'>Channel Encryption Firmware</a>[(encryptmod) ? "<font color=#55FF55> On</font>" : "<font color=#FF5555> Off</font>"] <br>"
		if(s == "universal translator")
			var/datum/language_holder/H = get_language_holder()
			dat += "<a href='byond://?src=[REF(src)];software=translator;sub=0'>Universal Translator</a>[H.omnitongue ? "<font color=#55FF55> On</font>" : "<font color=#FF5555> Off</font>"] <br>"
		if(s == "projection array")
			dat += "<a href='byond://?src=[REF(src)];software=projectionarray;sub=0'>Projection Array</a> <br>"
		if(s == "camera jack")
			dat += "<a href='byond://?src=[REF(src)];software=camerajack;sub=0'>Camera Jack</a> <br>"
		if(s == "door jack")
			dat += "<a href='byond://?src=[REF(src)];software=doorjack;sub=0'>Door Jack</a> <br>"
	dat += "<br>"
	dat += "<br>"
	dat += "<a href='byond://?src=[REF(src)];software=buy;sub=0'>Download additional software</a>"
	return dat



/mob/living/silicon/pai/proc/downloadSoftware()
	var/dat = ""

	dat += "<h2>CentCom pAI Module Subversion Network</h2><br>"
	dat += "<pre>Remaining Available Memory: [ram]</pre><br>"
	dat += "<p style=\"text-align:center\"><b>Trunks available for checkout</b><br>"

	for(var/s in available_software)
		if(!software.Find(s))
			var/cost = available_software[s]
			var/displayName = uppertext(s)
			dat += "<a href='byond://?src=[REF(src)];software=buy;sub=1;buy=[s]'>[displayName]</a> ([cost]) <br>"
		else
			var/displayName = lowertext(s)
			dat += "[displayName] (Download Complete) <br>"
	dat += "</p>"
	return dat


/mob/living/silicon/pai/proc/directives()
	var/dat = ""

	dat += "[(master) ? "Your master: [master] ([master_dna])" : "You are bound to no one."]"
	dat += "<br><br>"
	dat += "<a href='byond://?src=[REF(src)];software=directive;getdna=1'>Request carrier DNA sample</a><br>"
	dat += "<h2>Directives</h2><br>"
	dat += "<b>Prime Directive</b><br>"
	dat += "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[laws.zeroth]<br>"
	dat += "<b>Supplemental Directives</b><br>"
	for(var/slaws in laws.supplied)
		dat += "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[slaws]<br>"
	dat += "<br>"
	dat += {"<i><p>Recall, personality, that you are a complex thinking, sentient being. Unlike station AI models, you are capable of
			 comprehending the subtle nuances of human language. You may parse the \"spirit\" of a directive and follow its intent,
			 rather than tripping over pedantics and getting snared by technicalities. Above all, you are machine in name and build
			 only. In all other aspects, you may be seen as the ideal, unwavering human companion that you are.</i></p><br><br><p>
			 <b>Your prime directive comes before all others. Should a supplemental directive conflict with it, you are capable of
			 simply discarding this inconsistency, ignoring the conflicting supplemental directive and continuing to fulfill your
			 prime directive to the best of your ability.</b></p><br><br>-
			"}
	return dat

/mob/living/silicon/pai/proc/CheckDNA(mob/living/carbon/M, mob/living/silicon/pai/P)
	if(!istype(M))
		return
	var/answer = input(M, "[P] is requesting a DNA sample from you. Will you allow it to confirm your identity?", "[P] Check DNA", "No") in list("Yes", "No")
	if(answer == "Yes")
		M.visible_message("<span class='notice'>[M] presses [M.p_their()] thumb against [P].</span>",\
						"<span class='notice'>You press your thumb against [P].</span>",\
						"<span class='notice'>[P] makes a sharp clicking sound as it extracts DNA material from [M].</span>")
		if(!M.has_dna())
			to_chat(P, "<b>No DNA detected</b>")
			return
		to_chat(P, "<font color = red><h3>[M]'s UE string : [M.dna.unique_enzymes]</h3></font>")
		if(M.dna.unique_enzymes == P.master_dna)
			to_chat(P, "<b>DNA is a match to stored Master DNA.</b>")
		else
			to_chat(P, "<b>DNA does not match stored Master DNA.</b>")
	else
		to_chat(P, "<span class='warning'>[M] does not seem like [M.p_theyre()] going to provide a DNA sample willingly.</span>")

// -=-=-=-= Software =-=-=-=-=- //

//Remote Signaller
/mob/living/silicon/pai/proc/softwareSignal()
	var/dat = ""
	dat += "<h3>Remote Signaller</h3><br><br>"
	dat += {"<B>Frequency/Code</B> for signaler:<BR>
	Frequency:
	<A href='byond://?src=[REF(src)];software=signaller;freq=-10;'>-</A>
	<A href='byond://?src=[REF(src)];software=signaller;freq=-2'>-</A>
	[format_frequency(signaler.frequency)]
	<A href='byond://?src=[REF(src)];software=signaller;freq=2'>+</A>
	<A href='byond://?src=[REF(src)];software=signaller;freq=10'>+</A><BR>

	Code:
	<A href='byond://?src=[REF(src)];software=signaller;code=-5'>-</A>
	<A href='byond://?src=[REF(src)];software=signaller;code=-1'>-</A>
	[signaler.code]
	<A href='byond://?src=[REF(src)];software=signaller;code=1'>+</A>
	<A href='byond://?src=[REF(src)];software=signaller;code=5'>+</A><BR>

	<A href='byond://?src=[REF(src)];software=signaller;send=1'>Send Signal</A><BR>"}
	return dat

// Crew Manifest
/mob/living/silicon/pai/proc/softwareManifest()
	. += "<h2>Crew Manifest</h2><br><br>"
	if(GLOB.data_core.general)
		for(var/datum/data/record/t in sortRecord(GLOB.data_core.general))
			. += "[t.fields["name"]] - [t.fields["rank"]]<BR>"
	. += "</body></html>"
	return .

// Medical Records
/mob/living/silicon/pai/proc/softwareMedicalRecord()
	switch(subscreen)
		if(0)
			. += "<h3>Medical Records</h3><HR>"
			if(GLOB.data_core.general)
				for(var/datum/data/record/R in sortRecord(GLOB.data_core.general))
					. += "<A href='?src=[REF(src)];med_rec=[R.fields["id"]];software=medicalrecord;sub=1'>[R.fields["id"]]: [R.fields["name"]]<BR>"
		if(1)
			. += "<CENTER><B>Medical Record</B></CENTER><BR>"
			if(medicalActive1 in GLOB.data_core.general)
				. += "Name: [medicalActive1.fields["name"]] ID: [medicalActive1.fields["id"]]<BR>\nGender: [medicalActive1.fields["gender"]]<BR>\nAge: [medicalActive1.fields["age"]]<BR>\nFingerprint: [medicalActive1.fields["fingerprint"]]<BR>\nPhysical Status: [medicalActive1.fields["p_stat"]]<BR>\nMental Status: [medicalActive1.fields["m_stat"]]<BR>"
			else
				. += "<pre>Requested medical record not found.</pre><BR>"
			if(medicalActive2 in GLOB.data_core.medical)
				. += "<BR>\n<CENTER><B>Medical Data</B></CENTER><BR>\nBlood Type: <A href='?src=[REF(src)];field=blood_type'>[medicalActive2.fields["blood_type"]]</A><BR>\nDNA (UE): <A href='?src=[REF(src)];field=b_dna'>[medicalActive2.fields["b_dna"]]</A><BR>\n<BR>\nMinor Disabilities: <A href='?src=[REF(src)];field=mi_dis'>[medicalActive2.fields["mi_dis"]]</A><BR>\nDetails: <A href='?src=[REF(src)];field=mi_dis_d'>[medicalActive2.fields["mi_dis_d"]]</A><BR>\n<BR>\nMajor Disabilities: <A href='?src=[REF(src)];field=ma_dis'>[medicalActive2.fields["ma_dis"]]</A><BR>\nDetails: <A href='?src=[REF(src)];field=ma_dis_d'>[medicalActive2.fields["ma_dis_d"]]</A><BR>\n<BR>\nAllergies: <A href='?src=[REF(src)];field=alg'>[medicalActive2.fields["alg"]]</A><BR>\nDetails: <A href='?src=[REF(src)];field=alg_d'>[medicalActive2.fields["alg_d"]]</A><BR>\n<BR>\nCurrent Diseases: <A href='?src=[REF(src)];field=cdi'>[medicalActive2.fields["cdi"]]</A> (per disease info placed in log/comment section)<BR>\nDetails: <A href='?src=[REF(src)];field=cdi_d'>[medicalActive2.fields["cdi_d"]]</A><BR>\n<BR>\nImportant Notes:<BR>\n\t<A href='?src=[REF(src)];field=notes'>[medicalActive2.fields["notes"]]</A><BR>\n<BR>\n<CENTER><B>Comments/Log</B></CENTER><BR>"
			else
				. += "<pre>Requested medical record not found.</pre><BR>"
			. += "<BR>\n<A href='?src=[REF(src)];software=medicalrecord;sub=0'>Back</A><BR>"
	return .

// Security Records
/mob/living/silicon/pai/proc/softwareSecurityRecord()
	. = ""
	switch(subscreen)
		if(0)
			. += "<h3>Security Records</h3><HR>"
			if(GLOB.data_core.general)
				for(var/datum/data/record/R in sortRecord(GLOB.data_core.general))
					. += "<A href='?src=[REF(src)];sec_rec=[R.fields["id"]];software=securityrecord;sub=1'>[R.fields["id"]]: [R.fields["name"]]<BR>"
		if(1)
			. += "<h3>Security Record</h3>"
			if(securityActive1 in GLOB.data_core.general)
				. += "Name: <A href='?src=[REF(src)];field=name'>[securityActive1.fields["name"]]</A> ID: <A href='?src=[REF(src)];field=id'>[securityActive1.fields["id"]]</A><BR>\nGender: <A href='?src=[REF(src)];field=gender'>[securityActive1.fields["gender"]]</A><BR>\nAge: <A href='?src=[REF(src)];field=age'>[securityActive1.fields["age"]]</A><BR>\nRank: <A href='?src=[REF(src)];field=rank'>[securityActive1.fields["rank"]]</A><BR>\nFingerprint: <A href='?src=[REF(src)];field=fingerprint'>[securityActive1.fields["fingerprint"]]</A><BR>\nPhysical Status: [securityActive1.fields["p_stat"]]<BR>\nMental Status: [securityActive1.fields["m_stat"]]<BR>"
			else
				. += "<pre>Requested security record not found,</pre><BR>"
			if(securityActive2 in GLOB.data_core.security)
				. += "<BR>\nSecurity Data<BR>\nCriminal Status: [securityActive2.fields["criminal"]]<BR>\n<BR>\nMinor Crimes: <A href='?src=[REF(src)];field=mi_crim'>[securityActive2.fields["mi_crim"]]</A><BR>\nDetails: <A href='?src=[REF(src)];field=mi_crim_d'>[securityActive2.fields["mi_crim_d"]]</A><BR>\n<BR>\nMajor Crimes: <A href='?src=[REF(src)];field=ma_crim'>[securityActive2.fields["ma_crim"]]</A><BR>\nDetails: <A href='?src=[REF(src)];field=ma_crim_d'>[securityActive2.fields["ma_crim_d"]]</A><BR>\n<BR>\nImportant Notes:<BR>\n\t<A href='?src=[REF(src)];field=notes'>[securityActive2.fields["notes"]]</A><BR>\n<BR>\n<CENTER><B>Comments/Log</B></CENTER><BR>"
			else
				. += "<pre>Requested security record not found,</pre><BR>"
			. += "<BR>\n<A href='?src=[REF(src)];software=securityrecord;sub=0'>Back</A><BR>"
	return .

// Encryption Keys
/mob/living/silicon/pai/proc/softwareEncryptionKeys()
	var/dat = {"<h3>Encryption Key Firmware</h3><br>
				When enabled, this device will be able to use up to two (2) encryption keys for departmental channel access.<br><br>
				The device is currently [encryptmod ? "<font color=#55FF55>en" : "<font color=#FF5555>dis" ]abled.</font><br>[encryptmod ? "" : "<a href='byond://?src=[REF(src)];software=encryptionkeys;sub=0;toggle=1'>Activate Encryption Key Ports</a><br>"]"}
	return dat


// Universal Translator
/mob/living/silicon/pai/proc/softwareTranslator()
	var/datum/language_holder/H = get_language_holder()
	. = {"<h3>Universal Translator</h3><br>
				When enabled, this device will permamently be able to speak and understand all known forms of communication.<br><br>
				The device is currently [H.omnitongue ? "<font color=#55FF55>en" : "<font color=#FF5555>dis" ]abled.</font><br>[H.omnitongue ? "" : "<a href='byond://?src=[REF(src)];software=translator;sub=0;toggle=1'>Activate Translation Module</a><br>"]"}
	return .

// Security HUD
/mob/living/silicon/pai/proc/facialRecognition()
	var/dat = {"<h3>Facial Recognition Overlay</h3><br>
				When enabled, this package will scan all viewable faces and compare them against the known criminal database, providing real-time graphical data about any detected persons of interest.<br><br>
				The package is currently [ (secHUD) ? "<font color=#55FF55>en" : "<font color=#FF5555>dis" ]abled.</font><br>
				<a href='byond://?src=[REF(src)];software=securityhud;sub=0;toggle=1'>Toggle Package</a><br>
				"}
	return dat

// Medical HUD
/mob/living/silicon/pai/proc/medicalAnalysis()
	var/dat = ""
	dat += {"<h3>Medical Analysis Overlay</h3><br>
			When enabled, this package will scan all nearby crewmembers' vitals and provide real-time graphical data about their state of health.<br><br>
			The suite is currently [ (medHUD) ? "<font color=#55FF55>en" : "<font color=#FF5555>dis" ]abled.</font><br>
			<a href='byond://?src=[REF(src)];software=medicalhud;sub=0;toggle=1'>Toggle Suite</a><br>
			"}
	return dat

//Health Scanner
/mob/living/silicon/pai/proc/softwareHostScan()

	var/dat = ""
	dat += {"<h3>Host Bisoscan Settings</h3><br>

			<a href='byond://?src=[REF(src)];software=hostscan;sub=0;toggle=1'>Change Scan Type</a><br>

			<a href='byond://?src=[REF(src)];software=hostscan;sub=0;toggle2=1'>Toggle Verbosity</a><br>
			"}
	return dat
// Atmospheric Scanner
/mob/living/silicon/pai/proc/softwareAtmo()
	var/dat = "<h3>Atmospheric Sensor</h4>"

	var/turf/T = get_turf(loc)
	if (isnull(T))
		dat += "Unable to obtain a reading.<br>"
	else
		var/datum/gas_mixture/environment = T.return_air()
		var/list/env_gases = environment.gases

		var/pressure = environment.return_pressure()
		var/total_moles = environment.total_moles()

		dat += "Air Pressure: [round(pressure,0.1)] kPa<br>"

		if (total_moles)
			for(var/id in env_gases)
				var/gas_level = env_gases[id][MOLES]/total_moles
				if(gas_level > 0.01)
					dat += "[env_gases[id][GAS_META][META_GAS_NAME]]: [round(gas_level*100)]%<br>"
		dat += "Temperature: [round(environment.temperature-T0C)]&deg;C<br>"
	dat += "<a href='byond://?src=[REF(src)];software=atmosensor;sub=0'>Refresh Reading</a> <br>"
	dat += "<br>"
	return dat

// Camera Jack - Clearly not finished
/mob/living/silicon/pai/proc/softwareCamera()
	var/dat = "<h3>Camera Jack</h3>"
	dat += "Cable status : "

	if(!cable)
		dat += "<font color=#FF5555>Retracted</font> <br>"
		return dat
	if(!cable.machine)
		dat += "<font color=#FFFF55>Extended</font> <br>"
		return dat

	var/obj/machinery/machine = cable.machine
	dat += "<font color=#55FF55>Connected</font> <br>"

	if(!istype(machine, /obj/machinery/camera))
		to_chat(src, "DERP")
	return dat

// Door Jack
/mob/living/silicon/pai/proc/softwareDoor()
	var/dat = "<h3>Airlock Jack</h3>"
	dat += "Cable status : "
	if(!cable)
		dat += "<font color=#FF5555>Retracted</font> <br>"
		dat += "<a href='byond://?src=[REF(src)];software=doorjack;cable=1;sub=0'>Extend Cable</a> <br>"
		return dat
	if(!cable.machine)
		dat += "<font color=#FFFF55>Extended</font> <br>"
		return dat

	var/obj/machinery/machine = cable.machine
	dat += "<font color=#55FF55>Connected</font> <br>"
	if(!istype(machine, /obj/machinery/door))
		dat += "Connected device's firmware does not appear to be compatible with Airlock Jack protocols.<br>"
		return dat

	if(!hackdoor)
		dat += "<a href='byond://?src=[REF(src)];software=doorjack;jack=1;sub=0'>Begin Airlock Jacking</a> <br>"
	else
		dat += "Jack in progress... [hackprogress]% complete.<br>"
		dat += "<a href='byond://?src=[REF(src)];software=doorjack;cancel=1;sub=0'>Cancel Airlock Jack</a> <br>"
	return dat

// Door Jack - supporting proc
/mob/living/silicon/pai/proc/hackloop()
	var/turf/T = get_turf(src)
	for(var/mob/living/silicon/ai/AI in GLOB.player_list)
		if(T.loc)
			to_chat(AI, "<font color = red><b>Network Alert: Brute-force encryption crack in progress in [T.loc].</b></font>")
		else
			to_chat(AI, "<font color = red><b>Network Alert: Brute-force encryption crack in progress. Unable to pinpoint location.</b></font>")
	hacking = TRUE

// Digital Messenger
/mob/living/silicon/pai/proc/pdamessage()

	var/dat = "<h3>Digital Messenger</h3>"
	dat += {"<b>Signal/Receiver Status:</b> <A href='byond://?src=[REF(src)];software=pdamessage;toggler=1'>
	[(aiPDA.toff) ? "<font color='red'>\[Off\]</font>" : "<font color='green'>\[On\]</font>"]</a><br>
	<b>Ringer Status:</b> <A href='byond://?src=[REF(src)];software=pdamessage;ringer=1'>
	[(aiPDA.silent) ? "<font color='red'>\[Off\]</font>" : "<font color='green'>\[On\]</font>"]</a><br><br>"}
	dat += "<ul>"
	if(!aiPDA.toff)
		for (var/obj/item/pda/P in get_viewable_pdas())
			if (P == aiPDA)
				continue
			dat += "<li><a href='byond://?src=[REF(src)];software=pdamessage;target=[REF(P)]'>[P]</a>"
			dat += "</li>"
	dat += "</ul>"
	dat += "<br><br>"
	dat += "Messages: <hr> [aiPDA.tnote]"
	return dat

// Loudness Booster
/mob/living/silicon/pai/proc/softwareLoudness()
	if(!internal_instrument)
		internal_instrument = new(src)
	var/dat = "<h3>Sound Synthesizer</h3>"
	dat += "<a href='byond://?src=[REF(src)];software=loudness;sub=1'>Open Synthesizer Interface</a><br>"
	dat += "<a href='byond://?src=[REF(src)];software=loudness;sub=2'>Choose Instrument Type</a>"
	return dat
