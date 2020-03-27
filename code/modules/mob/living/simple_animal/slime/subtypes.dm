/mob/living/simple_animal/slime/proc/mutation_table(colour)
	var/list/slime_mutation[4]
	switch(colour)
		//Tier 1
		if("grey")
			slime_mutation[1] = "orange"
			slime_mutation[2] = "metal"
			slime_mutation[3] = "blue"
			slime_mutation[4] = "purple"
		//Tier 2
		if("purple")
			slime_mutation[1] = "dark purple"
			slime_mutation[2] = "dark blue"
			slime_mutation[3] = "green"
			slime_mutation[4] = "green"
		if("metal")
			slime_mutation[1] = "silver"
			slime_mutation[2] = "yellow"
			slime_mutation[3] = "gold"
			slime_mutation[4] = "gold"
		if("orange")
			slime_mutation[1] = "dark purple"
			slime_mutation[2] = "yellow"
			slime_mutation[3] = "red"
			slime_mutation[4] = "red"
		if("blue")
			slime_mutation[1] = "dark blue"
			slime_mutation[2] = "silver"
			slime_mutation[3] = "pink"
			slime_mutation[4] = "pink"
		//Tier 3
		if("dark blue")
			slime_mutation[1] = "purple"
			slime_mutation[2] = "blue"
			slime_mutation[3] = "cerulean"
			slime_mutation[4] = "cerulean"
		if("dark purple")
			slime_mutation[1] = "purple"
			slime_mutation[2] = "orange"
			slime_mutation[3] = "sepia"
			slime_mutation[4] = "sepia"
		if("yellow")
			slime_mutation[1] = "metal"
			slime_mutation[2] = "orange"
			slime_mutation[3] = "bluespace"
			slime_mutation[4] = "bluespace"
		if("silver")
			slime_mutation[1] = "metal"
			slime_mutation[2] = "blue"
			slime_mutation[3] = "pyrite"
			slime_mutation[4] = "pyrite"
		//Tier 4
		if("pink")
			slime_mutation[1] = "pink"
			slime_mutation[2] = "pink"
			slime_mutation[3] = "light pink"
			slime_mutation[4] = "light pink"
		if("red")
			slime_mutation[1] = "red"
			slime_mutation[2] = "red"
			slime_mutation[3] = "oil"
			slime_mutation[4] = "oil"
		if("gold")
			slime_mutation[1] = "gold"
			slime_mutation[2] = "gold"
			slime_mutation[3] = "adamantine"
			slime_mutation[4] = "adamantine"
		if("green")
			slime_mutation[1] = "green"
			slime_mutation[2] = "green"
			slime_mutation[3] = "black"
			slime_mutation[4] = "black"
		// Tier 5
		else
			slime_mutation[1] = colour
			slime_mutation[2] = colour
			slime_mutation[3] = colour
			slime_mutation[4] = colour
	return(slime_mutation)
