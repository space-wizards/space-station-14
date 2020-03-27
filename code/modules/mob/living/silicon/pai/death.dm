/mob/living/silicon/pai/death(gibbed)
	if(stat == DEAD)
		return
	set_stat(DEAD)
	mobility_flags = NONE
	update_sight()
	clear_fullscreens()

	//New pAI's get a brand new mind to prevent meta stuff from their previous life. This new mind causes problems down the line if it's not deleted here.
	GLOB.alive_mob_list -= src
	ghostize()
	qdel(src)
