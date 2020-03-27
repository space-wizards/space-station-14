
/datum/hud/blobbernaut/New(mob/owner)
	..()

	blobpwrdisplay = new /obj/screen/healths/blob/naut/core()
	blobpwrdisplay.hud = src
	infodisplay += blobpwrdisplay

	healths = new /obj/screen/healths/blob/naut()
	healths.hud = src
	infodisplay += healths
