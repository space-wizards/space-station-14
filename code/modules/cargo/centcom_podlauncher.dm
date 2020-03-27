//The Great and Mighty CentCom Pod Launcher - MrDoomBringer
//This was originally created as a way to get adminspawned items to the station in an IC manner. It's evolved to contain a few more
//features such as item removal, smiting, controllable delivery mobs, and more.

//This works by creating a supplypod (refered to as temp_pod) in a special room in the centcom map.
//IMPORTANT: Even though we call it a supplypod for our purposes, it can take on the appearance and function of many other things: Eg. cruise missiles, boxes, or walking, living gondolas.
//When the user launched the pod, items from special "bays" on the centcom map are taken and put into the supplypod

//The user can change properties of the supplypod using the UI, and change the way that items are taken from the bay (One at a time, ordered, random, etc)
//Many of the effects of the supplypod set here are put into action in supplypod.dm

/client/proc/centcom_podlauncher() //Creates a verb for admins to open up the ui
	set name = "Config/Launch Supplypod"
	set desc = "Configure and launch a Centcom supplypod full of whatever your heart desires!"
	set category = "Admin"
	var/datum/centcom_podlauncher/plaunch  = new(usr)//create the datum
	plaunch.ui_interact(usr)//datum has a tgui component, here we open the window

//Variables declared to change how items in the launch bay are picked and launched. (Almost) all of these are changed in the ui_act proc
//Some effect groups are choices, while other are booleans. This is because some effects can stack, while others dont (ex: you can stack explosion and quiet, but you cant stack ordered launch and random launch)
/datum/centcom_podlauncher
	var/static/list/ignored_atoms = typecacheof(list(null, /mob/dead, /obj/effect/landmark, /obj/docking_port, /atom/movable/lighting_object, /obj/effect/particle_effect/sparks, /obj/effect/DPtarget, /obj/effect/supplypod_selector ))
	var/turf/oldTurf //Keeps track of where the user was at if they use the "teleport to centcom" button, so they can go back
	var/client/holder //client of whoever is using this datum
	var/area/bay //What bay we're using to launch shit from.
	var/launchClone = FALSE //If true, then we don't actually launch the thing in the bay. Instead we call duplicateObject() and send the result
	var/launchChoice = 1 //Determines if we launch all at once (0) , in order (1), or at random(2)
	var/explosionChoice = 0 //Determines if there is no explosion (0), custom explosion (1), or just do a maxcap (2)
	var/damageChoice = 0 //Determines if we do no damage (0), custom amnt of damage (1), or gib + 5000dmg (2)
	var/launcherActivated = FALSE //check if we've entered "launch mode" (when we click a pod is launched). Used for updating mouse cursor
	var/effectBurst = FALSE //Effect that launches 5 at once in a 3x3 area centered on the target
	var/effectAnnounce = TRUE
	var/numTurfs = 0 //Counts the number of turfs with things we can launch in the chosen bay (in the centcom map)
	var/launchCounter = 1 //Used with the "Ordered" launch mode (launchChoice = 1) to see what item is launched
	var/atom/specificTarget //Do we want to target a specific mob instead of where we click? Also used for smiting
	var/list/orderedArea = list() //Contains an ordered list of turfs in an area (filled in the createOrderedArea() proc), read top-left to bottom-right. Used for the "ordered" launch mode (launchChoice = 1)
	var/list/turf/acceptableTurfs = list() //Contians a list of turfs (in the "bay" area on centcom) that have items that can be launched. Taken from orderedArea
	var/list/launchList = list() //Contains whatever is going to be put in the supplypod and fired. Taken from acceptableTurfs
	var/obj/effect/supplypod_selector/selector = new() //An effect used for keeping track of what item is going to be launched when in "ordered" mode (launchChoice = 1)
	var/obj/structure/closet/supplypod/centcompod/temp_pod //The temporary pod that is modified by this datum, then cloned. The buildObject() clone of this pod is what is launched

/datum/centcom_podlauncher/New(H)//H can either be a client or a mob due to byondcode(tm)
	if (istype(H,/client))
		var/client/C = H
		holder = C //if its a client, assign it to holder
	else
		var/mob/M = H
		holder = M.client //if its a mob, assign the mob's client to holder
	bay =  locate(/area/centcom/supplypod/loading/one) in GLOB.sortedAreas //Locate the default bay (one) from the centcom map
	temp_pod = new(locate(/area/centcom/supplypod/podStorage) in GLOB.sortedAreas) //Create a new temp_pod in the podStorage area on centcom (so users are free to look at it and change other variables if needed)
	orderedArea = createOrderedArea(bay) //Order all the turfs in the selected bay (top left to bottom right) to a single list. Used for the "ordered" mode (launchChoice = 1)

/datum/centcom_podlauncher/ui_interact(mob/user, ui_key = "main", datum/tgui/ui = null, \
force_open = FALSE, datum/tgui/master_ui = null, datum/ui_state/state = GLOB.admin_state)//ui_interact is called when the client verb is called.

	ui = SStgui.try_update_ui(user, src, ui_key, ui, force_open)
	if(!ui)
		ui = new(user, src, ui_key, "centcom_podlauncher", "Config/Launch Supplypod", 700, 700, master_ui, state)
		ui.open()

/datum/centcom_podlauncher/ui_data(mob/user) //Sends info about the pod to the UI.
	var/list/data = list() //*****NOTE*****: Many of these comments are similarly described in supplypod.dm. If you change them here, please consider doing so in the supplypod code as well!
	var/B = (istype(bay, /area/centcom/supplypod/loading/one)) ? 1 : (istype(bay, /area/centcom/supplypod/loading/two)) ? 2 : (istype(bay, /area/centcom/supplypod/loading/three)) ? 3 : (istype(bay, /area/centcom/supplypod/loading/four)) ? 4 : (istype(bay, /area/centcom/supplypod/loading/ert)) ? 5 : 0 //top ten THICCEST FUCKING TERNARY CONDITIONALS OF 2036
	data["bay"] = bay //Holds the current bay the user is launching objects from. Bays are specific rooms on the centcom map.
	data["bayNumber"] = B //Holds the bay as a number. Useful for comparisons in centcom_podlauncher.ract
	data["oldArea"] = (oldTurf ? get_area(oldTurf) : null) //Holds the name of the area that the user was in before using the teleportCentcom action
	data["launchClone"] = launchClone //Do we launch the actual items in the bay or just launch clones of them?
	data["launchChoice"] = launchChoice //Launch turfs all at once (0), ordered (1), or randomly(1)
	data["explosionChoice"] = explosionChoice //An explosion that occurs when landing. Can be no explosion (0), custom explosion (1), or maxcap (2)
	data["damageChoice"] = damageChoice //Damage that occurs to any mob under the pod when it lands. Can be no damage (0), custom damage (1), or gib+5000dmg (2)
	data["fallDuration"] = temp_pod.fallDuration //How long the pod's falling animation lasts
	data["landingDelay"] = temp_pod.landingDelay //How long the pod takes to land after launching
	data["openingDelay"] = temp_pod.openingDelay //How long the pod takes to open after landing
	data["departureDelay"] = temp_pod.departureDelay //How long the pod takes to leave after opening (if bluespace=true, it deletes. if reversing=true, it flies back to centcom)
	data["styleChoice"] = temp_pod.style //Style is a variable that keeps track of what the pod is supposed to look like. It acts as an index to the POD_STYLES list in cargo.dm defines to get the proper icon/name/desc for the pod.
	data["effectStun"] = temp_pod.effectStun //If true, stuns anyone under the pod when it launches until it lands, forcing them to get hit by the pod. Devilish!
	data["effectLimb"] = temp_pod.effectLimb //If true, pops off a limb (if applicable) from anyone caught under the pod when it lands
	data["effectOrgans"] = temp_pod.effectOrgans //If true, yeets the organs out of any bodies caught under the pod when it lands
	data["effectBluespace"] = temp_pod.bluespace //If true, the pod deletes (in a shower of sparks) after landing
	data["effectStealth"] = temp_pod.effectStealth //If true, a target icon isnt displayed on the turf where the pod will land
	data["effectQuiet"] = temp_pod.effectQuiet //The female sniper. If true, the pod makes no noise (including related explosions, opening sounds, etc)
	data["effectMissile"] = temp_pod.effectMissile //If true, the pod deletes the second it lands. If you give it an explosion, it will act like a missile exploding as it hits the ground
	data["effectCircle"] = temp_pod.effectCircle //If true, allows the pod to come in at any angle. Bit of a weird feature but whatever its here
	data["effectBurst"] = effectBurst //IOf true, launches five pods at once (with a very small delay between for added coolness), in a 3x3 area centered around the area
	data["effectReverse"] = temp_pod.reversing //If true, the pod will not send any items. Instead, after opening, it will close again (picking up items/mobs) and fly back to centcom
	data["effectTarget"] = specificTarget //Launches the pod at the turf of a specific mob target, rather than wherever the user clicked. Useful for smites
	data["effectName"] = temp_pod.adminNamed //Determines whether or not the pod has been named by an admin. If true, the pod's name will not get overridden when the style of the pod changes (changing the style of the pod normally also changes the name+desc)
	data["effectAnnounce"] = effectAnnounce
	data["giveLauncher"] = launcherActivated //If true, the user is in launch mode, and whenever they click a pod will be launched (either at their mouse position or at a specific target)
	data["numObjects"] = numTurfs //Counts the number of turfs that contain a launchable object in the centcom supplypod bay
	data["fallingSound"] = temp_pod.fallingSound != initial(temp_pod.fallingSound)//Admin sound to play as the pod falls
	data["landingSound"] = temp_pod.landingSound //Admin sound to play when the pod lands
	data["openingSound"] = temp_pod.openingSound //Admin sound to play when the pod opens
	data["leavingSound"] = temp_pod.leavingSound //Admin sound to play when the pod leaves
	data["soundVolume"] = temp_pod.soundVolume != initial(temp_pod.soundVolume) //Admin sound to play when the pod leaves
	return data

/datum/centcom_podlauncher/ui_act(action, params)
	if(..())
		return
	switch(action)
		////////////////////////////UTILITIES//////////////////
		if("bay1")
			bay =  locate(/area/centcom/supplypod/loading/one) in GLOB.sortedAreas //set the "bay" variable to the corresponding room in centcom
			refreshBay() //calls refreshBay() which "recounts" the bay to see what items we can launch (among other things).
			. = TRUE
		if("bay2")
			bay =  locate(/area/centcom/supplypod/loading/two) in GLOB.sortedAreas
			refreshBay()
			. = TRUE
		if("bay3")
			bay =  locate(/area/centcom/supplypod/loading/three) in GLOB.sortedAreas
			refreshBay()
			. = TRUE
		if("bay4")
			bay =  locate(/area/centcom/supplypod/loading/four) in GLOB.sortedAreas
			refreshBay()
			. = TRUE
		if("bay5")
			bay =  locate(/area/centcom/supplypod/loading/ert) in GLOB.sortedAreas
			refreshBay()
			. = TRUE
		if("teleportCentcom") //Teleports the user to the centcom supply loading facility.
			var/mob/M = holder.mob //We teleport whatever mob the client is attached to at the point of clicking
			oldTurf = get_turf(M) //Used for the "teleportBack" action
			var/area/A = locate(bay) in GLOB.sortedAreas
			var/list/turfs = list()
			for(var/turf/T in A)
				turfs.Add(T) //Fill a list with turfs in the area
			var/turf/T = safepick(turfs) //Only teleport if the list isn't empty
			if(!T) //If the list is empty, error and cancel
				to_chat(M, "Nowhere to jump to!")
				return
			M.forceMove(T) //Perform the actual teleport
			log_admin("[key_name(usr)] jumped to [AREACOORD(A)]")
			message_admins("[key_name_admin(usr)] jumped to [AREACOORD(A)]")
			. = TRUE
		if("teleportBack") //After teleporting to centcom, this button allows the user to teleport to the last spot they were at.
			var/mob/M = holder.mob
			if (!oldTurf) //If theres no turf to go back to, error and cancel
				to_chat(M, "Nowhere to jump to!")
				return
			M.forceMove(oldTurf) //Perform the actual teleport
			log_admin("[key_name(usr)] jumped to [AREACOORD(oldTurf)]")
			message_admins("[key_name_admin(usr)] jumped to [AREACOORD(oldTurf)]")
			. = TRUE

		////////////////////////////LAUNCH STYLE CHANGES//////////////////
		if("launchClone") //Toggles the launchClone var. See variable declarations above for what this specifically means
			launchClone = !launchClone
			. = TRUE
		if("launchOrdered") //Launch turfs (from the orderedArea list) one at a time in order, from the supplypod bay at centcom
			if (launchChoice == 1) //launchChoice 1 represents ordered. If we push "ordered" and it already is, then we go to default value
				launchChoice = 0
				updateSelector() //Move the selector effect to the next object that will be launched. See variable declarations for more info on the selector effect.
				return
			launchChoice = 1
			updateSelector()
			. = TRUE
		if("launchRandom") //Pick random turfs from the supplypod bay at centcom to launch
			if (launchChoice == 2)
				launchChoice = 0
				updateSelector()
				return
			launchChoice = 2
			updateSelector()
			. = TRUE

		////////////////////////////POD EFFECTS//////////////////
		if("explosionCustom") //Creates an explosion when the pod lands
			if (explosionChoice == 1) //If already a custom explosion, set to default (no explosion)
				explosionChoice = 0
				temp_pod.explosionSize = list(0,0,0,0)
				return
			var/list/expNames = list("Devastation", "Heavy Damage", "Light Damage", "Flame") //Explosions have a range of different types of damage
			var/list/boomInput = list()
			for (var/i=1 to expNames.len) //Gather input from the user for the value of each type of damage
				boomInput.Add(input("[expNames[i]] Range", "Enter the [expNames[i]] range of the explosion. WARNING: This ignores the bomb cap!", 0) as null|num)
				if (isnull(boomInput[i]))
					return
				if (!isnum(boomInput[i])) //If the user doesn't input a number, set that specific explosion value to zero
					alert(usr, "That wasnt a number! Value set to default (zero) instead.")
					boomInput = 0
			explosionChoice = 1
			temp_pod.explosionSize = boomInput
			. = TRUE
		if("explosionBus") //Creates a maxcap when the pod lands
			if (explosionChoice == 2) //If already a maccap, set to default (no explosion)
				explosionChoice = 0
				temp_pod.explosionSize = list(0,0,0,0)
				return
			explosionChoice = 2
			temp_pod.explosionSize = list(GLOB.MAX_EX_DEVESTATION_RANGE, GLOB.MAX_EX_HEAVY_RANGE, GLOB.MAX_EX_LIGHT_RANGE,GLOB.MAX_EX_FLAME_RANGE) //Set explosion to max cap of server
			. = TRUE
		if("damageCustom") //Deals damage to whoevers under the pod when it lands
			if (damageChoice == 1) //If already doing custom damage, set back to default (no damage)
				damageChoice = 0
				temp_pod.damage = 0
				return
			var/damageInput = input("How much damage to deal", "Enter the amount of brute damage dealt by getting hit", 0) as null|num
			if (isnull(damageInput))
				return
			if (!isnum(damageInput)) //Sanitize the input for damage to deal.s
				alert(usr, "That wasnt a number! Value set to default (zero) instead.")
				damageInput = 0
			damageChoice = 1
			temp_pod.damage = damageInput
			. = TRUE
		if("damageGib") //Gibs whoever is under the pod when it lands. Also deals 5000 damage, just to be sure.
			if (damageChoice == 2) //If already gibbing, set back to default (no damage)
				damageChoice = 0
				temp_pod.damage = 0
				temp_pod.effectGib = FALSE
				return
			damageChoice = 2
			temp_pod.damage = 5000
			temp_pod.effectGib = TRUE //Gibs whoever is under the pod when it lands
			. = TRUE
		if("effectName") //Give the supplypod a custom name. Supplypods automatically get their name based on their style (see supplypod/setStyle() proc), so doing this overrides that.
			if (temp_pod.adminNamed) //If we're already adminNamed, set the name of the pod back to default
				temp_pod.adminNamed = FALSE
				temp_pod.setStyle(temp_pod.style) //This resets the name of the pod based on it's current style (see supplypod/setStyle() proc)
				return
			var/nameInput= input("Custom name", "Enter a custom name", POD_STYLES[temp_pod.style][POD_NAME]) as null|text //Gather input for name and desc
			if (isnull(nameInput))
				return
			var/descInput = input("Custom description", "Enter a custom desc", POD_STYLES[temp_pod.style][POD_DESC]) as null|text //The POD_STYLES is used to get the name, desc, or icon state based on the pod's style
			if (isnull(descInput))
				return
			temp_pod.name = nameInput
			temp_pod.desc = descInput
			temp_pod.adminNamed = TRUE //This variable is checked in the supplypod/setStyle() proc
			. = TRUE
		if("effectStun") //Toggle: Any mob under the pod is stunned (cant move) until the pod lands, hitting them!
			temp_pod.effectStun = !temp_pod.effectStun
			. = TRUE
		if("effectLimb") //Toggle: Anyone carbon mob under the pod loses a limb when it lands
			temp_pod.effectLimb = !temp_pod.effectLimb
			. = TRUE
		if("effectOrgans") //Toggle: Anyone carbon mob under the pod loses a limb when it lands
			temp_pod.effectOrgans = !temp_pod.effectOrgans
			. = TRUE
		if("effectBluespace") //Toggle: Deletes the pod after landing
			temp_pod.bluespace = !temp_pod.bluespace
			. = TRUE
		if("effectStealth") //Toggle: There is no red target indicator showing where the pod will land
			temp_pod.effectStealth = !temp_pod.effectStealth
			. = TRUE
		if("effectQuiet") //Toggle: The pod makes no noise (explosions, opening sounds, etc)
			temp_pod.effectQuiet = !temp_pod.effectQuiet
			. = TRUE
		if("effectMissile") //Toggle: The pod deletes the instant it lands. Looks nicer than just setting the open delay and leave delay to zero. Useful for combo-ing with explosions
			temp_pod.effectMissile = !temp_pod.effectMissile
			. = TRUE
		if("effectCircle") //Toggle: The pod can come in from any descent angle. Goof requested this im not sure why but it looks p funny actually
			temp_pod.effectCircle = !temp_pod.effectCircle
			. = TRUE
		if("effectBurst") //Toggle: Launch 5 pods (with a very slight delay between) in a 3x3 area centered around the target
			effectBurst = !effectBurst
			. = TRUE
		if("effectAnnounce") //Toggle: Launch 5 pods (with a very slight delay between) in a 3x3 area centered around the target
			effectAnnounce = !effectAnnounce
			. = TRUE
		if("effectReverse") //Toggle: Don't send any items. Instead, after landing, close (taking any objects inside) and go back to the centcom bay it came from
			temp_pod.reversing = !temp_pod.reversing
			. = TRUE
		if("effectTarget") //Toggle: Launch at a specific mob (instead of at whatever turf you click on). Used for the supplypod smite
			if (specificTarget)
				specificTarget = null
				return
			var/list/mobs = getpois()//code stolen from observer.dm
			var/inputTarget = input("Select a mob! (Smiting does this automatically)", "Target", null, null) as null|anything in mobs
			if (isnull(inputTarget))
				return
			var/mob/target = mobs[inputTarget]
			specificTarget = target///input specific tartget
			. = TRUE

		////////////////////////////TIMER DELAYS//////////////////
		if("fallDuration") //Change the time it takes the pod to land, after firing
			if (temp_pod.fallDuration != initial(temp_pod.fallDuration)) //If the landing delay has already been changed when we push the "change value" button, then set it to default
				temp_pod.fallDuration = initial(temp_pod.fallDuration)
				return
			var/timeInput = input("Enter the duration of the pod's falling animation, in seconds", "Delay Time",  initial(temp_pod.fallDuration) * 0.1) as null|num
			if (isnull(timeInput))
				return
			if (!isnum(timeInput)) //Sanitize input, if it doesnt check out, error and set to default
				alert(usr, "That wasnt a number! Value set to default ([initial(temp_pod.fallDuration)*0.1]) instead.")
				timeInput = initial(temp_pod.fallDuration)
			temp_pod.fallDuration = 10 * timeInput
			. = TRUE
		if("landingDelay") //Change the time it takes the pod to land, after firing
			if (temp_pod.landingDelay != initial(temp_pod.landingDelay)) //If the landing delay has already been changed when we push the "change value" button, then set it to default
				temp_pod.landingDelay = initial(temp_pod.landingDelay)
				return
			var/timeInput = input("Enter the time it takes for the pod to land, in seconds", "Delay Time", initial(temp_pod.landingDelay) * 0.1) as null|num
			if (isnull(timeInput))
				return
			if (!isnum(timeInput)) //Sanitize input, if it doesnt check out, error and set to default
				alert(usr, "That wasnt a number! Value set to default ([initial(temp_pod.landingDelay)*0.1]) instead.")
				timeInput = initial(temp_pod.landingDelay)
			temp_pod.landingDelay = 10 * timeInput
			. = TRUE
		if("openingDelay") //Change the time it takes the pod to open it's door (and release its contents) after landing
			if (temp_pod.openingDelay != initial(temp_pod.openingDelay)) //If the opening delay has already been changed when we push the "change value" button, then set it to default
				temp_pod.openingDelay = initial(temp_pod.openingDelay)
				return
			var/timeInput = input("Enter the time it takes for the pod to open after landing, in seconds", "Delay Time", initial(temp_pod.openingDelay) * 0.1) as null|num
			if (isnull(timeInput))
				return
			if (!isnum(timeInput)) //Sanitize input
				alert(usr, "That wasnt a number! Value set to default ([initial(temp_pod.openingDelay)*0.1]) instead.")
				timeInput = initial(temp_pod.openingDelay)
			temp_pod.openingDelay = 10 *  timeInput
			. = TRUE
		if("departureDelay") //Change the time it takes the pod to leave (if bluespace = true it just deletes, if effectReverse = true it goes back to centcom)
			if (temp_pod.departureDelay != initial(temp_pod.departureDelay)) //If the departure delay has already been changed when we push the "change value" button, then set it to default
				temp_pod.departureDelay = initial(temp_pod.departureDelay)
				return
			var/timeInput = input("Enter the time it takes for the pod to leave after opening, in seconds", "Delay Time", initial(temp_pod.departureDelay) * 0.1) as null|num
			if (isnull(timeInput))
				return
			if (!isnum(timeInput))
				alert(usr, "That wasnt a number! Value set to default ([initial(temp_pod.departureDelay)*0.1]) instead.")
				timeInput = initial(temp_pod.departureDelay)
			temp_pod.departureDelay = 10 * timeInput
			. = TRUE

		////////////////////////////ADMIN SOUNDS//////////////////
		if("fallingSound") //Admin sound from a local file that plays when the pod lands
			if ((temp_pod.fallingSound) != initial(temp_pod.fallingSound))
				temp_pod.fallingSound = initial(temp_pod.fallingSound)
				temp_pod.fallingSoundLength = initial(temp_pod.fallingSoundLength)
				return
			var/soundInput = input(holder, "Please pick a sound file to play when the pod lands! NOTICE: Take a note of exactly how long the sound is.", "Pick a Sound File") as null|sound
			if (isnull(soundInput))
				return
			var/timeInput =  input(holder, "What is the exact length of the sound file, in seconds. This number will be used to line the sound up so that it finishes right as the pod lands!", "Pick a Sound File", 0.3) as null|num
			if (isnull(timeInput))
				return
			if (!isnum(timeInput))
				alert(usr, "That wasnt a number! Value set to default ([initial(temp_pod.fallingSoundLength)*0.1]) instead.")
			temp_pod.fallingSound = soundInput
			temp_pod.fallingSoundLength = 10 * timeInput
			. = TRUE
		if("landingSound") //Admin sound from a local file that plays when the pod lands
			if (!isnull(temp_pod.landingSound))
				temp_pod.landingSound = null
				return
			var/soundInput = input(holder, "Please pick a sound file to play when the pod lands! I reccomend a nice \"oh shit, i'm sorry\", incase you hit someone with the pod.", "Pick a Sound File") as null|sound
			if (isnull(soundInput))
				return
			temp_pod.landingSound = soundInput
			. = TRUE
		if("openingSound") //Admin sound from a local file that plays when the pod opens
			if (!isnull(temp_pod.openingSound))
				temp_pod.openingSound = null
				return
			var/soundInput = input(holder, "Please pick a sound file to play when the pod opens! I reccomend a stock sound effect of kids cheering at a party, incase your pod is full of fun exciting stuff!", "Pick a Sound File") as null|sound
			if (isnull(soundInput))
				return
			temp_pod.openingSound = soundInput
			. = TRUE
		if("leavingSound") //Admin sound from a local file that plays when the pod leaves
			if (!isnull(temp_pod.leavingSound))
				temp_pod.leavingSound = null
				return
			var/soundInput = input(holder, "Please pick a sound file to play when the pod leaves! I reccomend a nice slide whistle sound, especially if you're using the reverse pod effect.", "Pick a Sound File") as null|sound
			if (isnull(soundInput))
				return
			temp_pod.leavingSound = soundInput
			. = TRUE
		if("soundVolume") //Admin sound from a local file that plays when the pod leaves
			if (temp_pod.soundVolume != initial(temp_pod.soundVolume))
				temp_pod.soundVolume = initial(temp_pod.soundVolume)
				return
			var/soundInput = input(holder, "Please pick a volume. Default is between 1 and 100 with 50 being average, but pick whatever. I'm a notification, not a cop. If you still cant hear your sound, consider turning on the Quiet effect. It will silence all pod sounds except for the custom admin ones set by the previous three buttons.", "Pick Admin Sound Volume") as null|num
			if (isnull(soundInput))
				return
			temp_pod.soundVolume = soundInput
			. = TRUE
		////////////////////////////STYLE CHANGES//////////////////
		//Style is a value that is used to keep track of what the pod is supposed to look like. It can be used with the POD_STYLES list (in cargo.dm defines)
		//as a way to get the proper icon state, name, and description of the pod.
		if("styleStandard")
			temp_pod.setStyle(STYLE_STANDARD)
			. = TRUE
		if("styleBluespace")
			temp_pod.setStyle(STYLE_BLUESPACE)
			. = TRUE
		if("styleSyndie")
			temp_pod.setStyle(STYLE_SYNDICATE)
			. = TRUE
		if("styleBlue")
			temp_pod.setStyle(STYLE_BLUE)
			. = TRUE
		if("styleCult")
			temp_pod.setStyle(STYLE_CULT)
			. = TRUE
		if("styleMissile")
			temp_pod.setStyle(STYLE_MISSILE)
			. = TRUE
		if("styleSMissile")
			temp_pod.setStyle(STYLE_RED_MISSILE)
			. = TRUE
		if("styleBox")
			temp_pod.setStyle(STYLE_BOX)
			. = TRUE
		if("styleHONK")
			temp_pod.setStyle(STYLE_HONK)
			. = TRUE
		if("styleFruit")
			temp_pod.setStyle(STYLE_FRUIT)
			. = TRUE
		if("styleInvisible")
			temp_pod.setStyle(STYLE_INVISIBLE)
			. = TRUE
		if("styleGondola")
			temp_pod.setStyle(STYLE_GONDOLA)
			. = TRUE
		if("styleSeeThrough")
			temp_pod.setStyle(STYLE_SEETHROUGH)
			. = TRUE
		if("refresh") //Refresh the Pod bay. User should press this if they spawn something new in the centcom bay. Automatically called whenever the user launches a pod
			refreshBay()
			. = TRUE
		if("giveLauncher") //Enters the "Launch Mode". When the launcher is activated, temp_pod is cloned, and the result it filled and launched anywhere the user clicks (unless specificTarget is true)
			launcherActivated = !launcherActivated
			updateCursor(launcherActivated) //Update the cursor of the user to a cool looking target icon
			. = TRUE
		if("clearBay") //Delete all mobs and objs in the selected bay
			if(alert(usr, "This will delete all objs and mobs in [bay]. Are you sure?", "Confirmation", "Delete that shit", "No") == "Delete that shit")
				clearBay()
				refreshBay()
			. = TRUE

/datum/centcom_podlauncher/ui_close() //Uses the destroy() proc. When the user closes the UI, we clean up the temp_pod and supplypod_selector variables.
	qdel(src)

/datum/centcom_podlauncher/proc/updateCursor(var/launching) //Update the moues of the user
	if (holder) //Check to see if we have a client
		if (launching) //If the launching param is true, we give the user new mouse icons.
			holder.mouse_up_icon = 'icons/effects/supplypod_target.dmi' //Icon for when mouse is released
			holder.mouse_down_icon = 'icons/effects/supplypod_down_target.dmi' //Icon for when mouse is pressed
			holder.mouse_pointer_icon = holder.mouse_up_icon //Icon for idle mouse (same as icon for when released)
			holder.click_intercept = src //Create a click_intercept so we know where the user is clicking
		else
			var/mob/M = holder.mob
			holder.mouse_up_icon = null
			holder.mouse_down_icon = null
			holder.click_intercept = null
			if (M)
				M.update_mouse_pointer() //set the moues icons to null, then call update_moues_pointer() which resets them to the correct values based on what the mob is doing (in a mech, holding a spell, etc)()

/datum/centcom_podlauncher/proc/InterceptClickOn(user,params,atom/target) //Click Intercept so we know where to send pods where the user clicks
	var/list/pa = params2list(params)
	var/left_click = pa.Find("left")
	if (launcherActivated)
		//Clicking on UI elements shouldn't launch a pod
		if(istype(target,/obj/screen))
			return FALSE

		. = TRUE

		if(left_click) //When we left click:
			preLaunch() //Fill the acceptableTurfs list from the orderedArea list. Then, fill up the launchList list with items from the acceptableTurfs list based on the manner of launch (ordered, random, etc)
			if (!isnull(specificTarget))
				target = get_turf(specificTarget) //if we have a specific target, then always launch the pod at the turf of the target
			else if (target)
				target = get_turf(target) //Make sure we're aiming at a turf rather than an item or effect or something
			else
				return //if target is null and we don't have a specific target, cancel
			if (effectAnnounce)
				deadchat_broadcast("A special package is being launched at the station!", turf_target = target)
			var/list/bouttaDie = list()
			for (var/mob/living/M in target)
				bouttaDie.Add(M)
			supplypod_punish_log(bouttaDie)
			if (!effectBurst) //If we're not using burst mode, just launch normally.
				launch(target)
			else
				for (var/i in 1 to 5) //If we're using burst mode, launch 5 pods
					if (isnull(target))
						break //if our target gets deleted during this, we stop the show
					preLaunch() //Same as above
					var/LZ = locate(target.x + rand(-1,1), target.y + rand(-1,1), target.z) //Pods are randomly adjacent to (or the same as) the target
					if (LZ) //just incase we're on the edge of the map or something that would cause target.x+1 to fail
						launch(LZ) //launch the pod at the adjacent turf
					else
						launch(target) //If we couldn't locate an adjacent turf, just launch at the normal target
					sleep(rand()*2) //looks cooler than them all appearing at once. Gives the impression of burst fire.

/datum/centcom_podlauncher/proc/refreshBay() //Called whenever the bay is switched, as well as wheneber a pod is launched
	orderedArea = createOrderedArea(bay) //Create an ordered list full of turfs form the bay
	preLaunch()	//Fill acceptable turfs from orderedArea, then fill launchList from acceptableTurfs (see proc for more info)

/datum/centcom_podlauncher/proc/createOrderedArea(area/A) //This assumes the area passed in is a continuous square
	if (isnull(A)) //If theres no supplypod bay mapped into centcom, throw an error
		to_chat(holder.mob, "No /area/centcom/supplypod/loading/one (or /two or /three or /four) in the world! You can make one yourself (then refresh) for now, but yell at a mapper to fix this, today!")
		CRASH("No /area/centcom/supplypod/loading/one (or /two or /three or /four) has been mapped into the centcom z-level!")
	orderedArea = list()
	if (!isemptylist(A.contents)) //Go through the area passed into the proc, and figure out the top left and bottom right corners by calculating max and min values
		var/startX = A.contents[1].x //Create the four values (we do it off a.contents[1] so they have some sort of arbitrary initial value. They should be overwritten in a few moments)
		var/endX = A.contents[1].x
		var/startY = A.contents[1].y
		var/endY = A.contents[1].y
		for (var/turf/T in A) //For each turf in the area, go through and find:
			if (T.x < startX) //The turf with the smallest x value. This is our startX
				startX = T.x
			else if (T.x > endX) //The turf with the largest x value. This is our endX
				endX = T.x
			else if (T.y > startY) //The turf with the largest Y value. This is our startY
				startY = T.y
			else if (T.y < endY) //The turf with the smallest Y value. This is our endY
				endY = T.y
		for (var/i in endY to startY)
			for (var/j in startX to endX)
				orderedArea.Add(locate(j,startY - (i - endY),1)) //After gathering the start/end x and y, go through locating each turf from top left to bottom right, like one would read a book
	return orderedArea //Return the filled list

/datum/centcom_podlauncher/proc/preLaunch() //Creates a list of acceptable items,
	numTurfs = 0 //Counts the number of turfs that can be launched (remember, supplypods either launch all at once or one turf-worth of items at a time)
	acceptableTurfs = list()
	for (var/turf/T in orderedArea) //Go through the orderedArea list
		if (typecache_filter_list_reverse(T.contents, ignored_atoms).len != 0) //if there is something in this turf that isnt in the blacklist, we consider this turf "acceptable" and add it to the acceptableTurfs list
			acceptableTurfs.Add(T) //Because orderedArea was an ordered linear list, acceptableTurfs will be as well.
			numTurfs ++

	launchList = list() //Anything in launchList will go into the supplypod when it is launched
	if (!isemptylist(acceptableTurfs) && !temp_pod.reversing && !temp_pod.effectMissile) //We dont fill the supplypod if acceptableTurfs is empty, if the pod is going in reverse (effectReverse=true), or if the pod is acitng like a missile (effectMissile=true)
		switch(launchChoice)
			if(0) //If we are launching all the turfs at once
				for (var/turf/T in acceptableTurfs)
					launchList |= typecache_filter_list_reverse(T.contents, ignored_atoms) //We filter any blacklisted atoms and add the rest to the launchList
			if(1) //If we are launching one at a time
				if (launchCounter > acceptableTurfs.len) //Check if the launchCounter, which acts as an index, is too high. If it is, reset it to 1
					launchCounter = 1 //Note that the launchCounter index is incremented in the launch() proc
				for (var/atom/movable/O in acceptableTurfs[launchCounter].contents) //Go through the acceptableTurfs list based on the launchCounter index
					launchList |= typecache_filter_list_reverse(acceptableTurfs[launchCounter].contents, ignored_atoms) //Filter the specicic turf chosen from acceptableTurfs, and add it to the launchList
			if(2) //If we are launching randomly
				launchList |= typecache_filter_list_reverse(pick_n_take(acceptableTurfs).contents, ignored_atoms) //filter a random turf from the acceptableTurfs list and add it to the launchList
	updateSelector() //Call updateSelector(), which, if we are launching one at a time (launchChoice==2), will move to the next turf that will be launched
	//UpdateSelector() is here (instead if the if(1) switch block) because it also moves the selector to nullspace (to hide it) if needed

/datum/centcom_podlauncher/proc/launch(turf/A) //Game time started
	if (isnull(A))
		return
	var/obj/structure/closet/supplypod/centcompod/toLaunch = DuplicateObject(temp_pod) //Duplicate the temp_pod (which we have been varediting or configuring with the UI) and store the result
	toLaunch.bay = bay //Bay is currently a nonstatic expression, so it cant go into toLaunch using DuplicateObject
	toLaunch.update_icon()//we update_icon() here so that the door doesnt "flicker on" right after it lands
	var/shippingLane = GLOB.areas_by_type[/area/centcom/supplypod/flyMeToTheMoon]
	toLaunch.forceMove(shippingLane)
	if (launchClone) //We arent launching the actual items from the bay, rather we are creating clones and launching those
		for (var/atom/movable/O in launchList)
			DuplicateObject(O).forceMove(toLaunch) //Duplicate each atom/movable in launchList and forceMove them into the supplypod
		new /obj/effect/DPtarget(A, toLaunch) //Create the DPTarget, which will eventually forceMove the temp_pod to it's location
	else
		for (var/atom/movable/O in launchList) //If we aren't cloning the objects, just go through the launchList
			O.forceMove(toLaunch) //and forceMove any atom/moveable into the supplypod
		new /obj/effect/DPtarget(A, toLaunch) //Then, create the DPTarget effect, which will eventually forceMove the temp_pod to it's location
	if (launchClone)
		launchCounter++ //We only need to increment launchCounter if we are cloning objects.
		//If we aren't cloning objects, taking and removing the first item each time from the acceptableTurfs list will inherently iterate through the list in order

/datum/centcom_podlauncher/proc/updateSelector() //Ensures that the selector effect will showcase the next item if needed
	if (launchChoice == 1 && !isemptylist(acceptableTurfs) && !temp_pod.reversing && !temp_pod.effectMissile) //We only show the selector if we are taking items from the bay
		var/index = launchCounter + 1 //launchCounter acts as an index to the ordered acceptableTurfs list, so adding one will show the next item in the list
		if (index > acceptableTurfs.len) //out of bounds check
			index = 1
		selector.forceMove(acceptableTurfs[index]) //forceMove the selector to the next turf in the ordered acceptableTurfs list
	else
		selector.moveToNullspace() //Otherwise, we move the selector to nullspace until it is needed again

/datum/centcom_podlauncher/proc/clearBay() //Clear all objs and mobs from the selected bay
	for (var/obj/O in bay.GetAllContents())
		qdel(O)
	for (var/mob/M in bay.GetAllContents())
		qdel(M)

/datum/centcom_podlauncher/Destroy() //The Destroy() proc. This is called by ui_close proc, or whenever the user leaves the game
	updateCursor(FALSE) //Make sure our moues cursor resets to default. False means we are not in launch mode
	qdel(temp_pod) //Delete the temp_pod
	qdel(selector) //Delete the selector effect
	. = ..()

/datum/centcom_podlauncher/proc/supplypod_punish_log(var/list/whoDyin)
	var/podString = effectBurst ? "5 pods" : "a pod"
	var/whomString = ""
	if (LAZYLEN(whoDyin))
		for (var/mob/living/M in whoDyin)
			whomString += "[key_name(M)], "

	var/delayString = temp_pod.landingDelay == initial(temp_pod.landingDelay) ? "" : " Delay=[temp_pod.landingDelay*0.1]s"
	var/damageString = temp_pod.damage == 0 ? "" : " Dmg=[temp_pod.damage]"
	var/explosionString = ""
	var/explosion_sum = temp_pod.explosionSize[1] + temp_pod.explosionSize[2] + temp_pod.explosionSize[3] + temp_pod.explosionSize[4]
	if (explosion_sum != 0)
		explosionString = " Boom=|"
		for (var/X in temp_pod.explosionSize)
			explosionString += "[X]|"

	var/msg = "launched [podString] towards [whomString] [delayString][damageString][explosionString]"
	message_admins("[key_name_admin(usr)] [msg] in [ADMIN_VERBOSEJMP(specificTarget)].")
	if (!isemptylist(whoDyin))
		for (var/mob/living/M in whoDyin)
			admin_ticket_log(M, "[key_name_admin(usr)] [msg]")
