/datum/space_level/proc/set_linkage(new_linkage)
	linkage = new_linkage
	if(linkage == SELFLOOPING)
		neigbours = list(TEXT_NORTH,TEXT_SOUTH,TEXT_EAST,TEXT_WEST)
		for(var/A in neigbours)
			neigbours[A] = src

/datum/space_level/proc/set_neigbours(list/L)
	for(var/datum/space_transition_point/P in L)
		if(P.x == xi)
			if(P.y == yi+1)
				neigbours[TEXT_NORTH] = P.spl
				P.spl.neigbours[TEXT_SOUTH] = src
			else if(P.y == yi-1)
				neigbours[TEXT_SOUTH] = P.spl
				P.spl.neigbours[TEXT_NORTH] = src
		else if(P.y == yi)
			if(P.x == xi+1)
				neigbours[TEXT_EAST] = P.spl
				P.spl.neigbours[TEXT_WEST] = src
			else if(P.x == xi-1)
				neigbours[TEXT_WEST] = P.spl
				P.spl.neigbours[TEXT_EAST] = src

/datum/space_transition_point          //this is explicitly utilitarian datum type made specially for the space map generation and are absolutely unusable for anything else
	var/list/neigbours = list()
	var/x
	var/y
	var/datum/space_level/spl

/datum/space_transition_point/New(nx, ny, list/point_grid)
	if(!point_grid)
		qdel(src)
		return
	var/list/L = point_grid[1]
	if(nx > point_grid.len || ny > L.len)
		qdel(src)
		return
	x = nx
	y = ny
	if(point_grid[x][y])
		return
	point_grid[x][y] = src

/datum/space_transition_point/proc/set_neigbours(list/grid)
	var/max_X = grid.len
	var/list/max_Y = grid[1]
	max_Y = max_Y.len
	neigbours.Cut()
	if(x+1 <= max_X)
		neigbours |= grid[x+1][y]
	if(x-1 >= 1)
		neigbours |= grid[x-1][y]
	if(y+1 <= max_Y)
		neigbours |= grid[x][y+1]
	if(y-1 >= 1)
		neigbours |= grid[x][y-1]

/datum/controller/subsystem/mapping/proc/setup_map_transitions() //listamania
	var/list/SLS = list()
	var/list/cached_z_list = z_list
	var/conf_set_len = 0
	for(var/A in cached_z_list)
		var/datum/space_level/D = A
		if (D.linkage == CROSSLINKED)
			SLS.Add(D)
		conf_set_len++
	var/list/point_grid[conf_set_len*2+1][conf_set_len*2+1]
	var/list/grid = list()
	var/datum/space_transition_point/P
	for(var/i = 1, i<=conf_set_len*2+1, i++)
		for(var/j = 1, j<=conf_set_len*2+1, j++)
			P = new/datum/space_transition_point(i,j, point_grid)
			point_grid[i][j] = P
			grid.Add(P)
	for(var/datum/space_transition_point/pnt in grid)
		pnt.set_neigbours(point_grid)
	P = point_grid[conf_set_len+1][conf_set_len+1]
	var/list/possible_points = list()
	var/list/used_points = list()
	grid.Cut()
	while(SLS.len)
		var/datum/space_level/D = pick_n_take(SLS)
		D.xi = P.x
		D.yi = P.y
		P.spl = D
		possible_points |= P.neigbours
		used_points |= P
		possible_points.Remove(used_points)
		D.set_neigbours(used_points)
		P = pick(possible_points)
		CHECK_TICK

	//Lists below are pre-calculated values arranged in the list in such a way to be easily accessable in the loop by the counter
	//Its either this or madness with lotsa math

	var/list/x_pos_beginning = list(1, 1, world.maxx - TRANSITIONEDGE, 1)  //x values of the lowest-leftest turfs of the respective 4 blocks on each side of zlevel
	var/list/y_pos_beginning = list(world.maxy - TRANSITIONEDGE, 1, 1 + TRANSITIONEDGE, 1 + TRANSITIONEDGE)  //y values respectively
	var/list/x_pos_ending = list(world.maxx, world.maxx, world.maxx, 1 + TRANSITIONEDGE)	//x values of the highest-rightest turfs of the respective 4 blocks on each side of zlevel
	var/list/y_pos_ending = list(world.maxy, 1 + TRANSITIONEDGE, world.maxy - TRANSITIONEDGE, world.maxy - TRANSITIONEDGE)	//y values respectively
	var/list/x_pos_transition = list(1, 1, TRANSITIONEDGE + 2, world.maxx - TRANSITIONEDGE - 1)		//values of x for the transition from respective blocks on the side of zlevel, 1 is being translated into turfs respective x value later in the code
	var/list/y_pos_transition = list(TRANSITIONEDGE + 2, world.maxy - TRANSITIONEDGE - 1, 1, 1)		//values of y for the transition from respective blocks on the side of zlevel, 1 is being translated into turfs respective y value later in the code

	for(var/I in cached_z_list)
		var/datum/space_level/D = I
		if(!D.neigbours.len)
			continue
		var/zlevelnumber = D.z_value
		for(var/side in 1 to 4)
			var/turf/beginning = locate(x_pos_beginning[side], y_pos_beginning[side], zlevelnumber)
			var/turf/ending = locate(x_pos_ending[side], y_pos_ending[side], zlevelnumber)
			var/list/turfblock = block(beginning, ending)
			var/dirside = 2**(side-1)
			var/zdestination = zlevelnumber
			if(D.neigbours["[dirside]"] && D.neigbours["[dirside]"] != D)
				D = D.neigbours["[dirside]"]
				zdestination = D.z_value
			else
				dirside = turn(dirside, 180)
				while(D.neigbours["[dirside]"] && D.neigbours["[dirside]"] != D)
					D = D.neigbours["[dirside]"]
				zdestination = D.z_value
			D = I
			for(var/turf/open/space/S in turfblock)
				S.destination_x = x_pos_transition[side] == 1 ? S.x : x_pos_transition[side]
				S.destination_y = y_pos_transition[side] == 1 ? S.y : y_pos_transition[side]
				S.destination_z = zdestination
				
				// Mirage border code
				var/mirage_dir
				if(S.x == 1 + TRANSITIONEDGE)
					mirage_dir |= WEST
				else if(S.x == world.maxx - TRANSITIONEDGE)
					mirage_dir |= EAST
				if(S.y == 1 + TRANSITIONEDGE)
					mirage_dir |= SOUTH
				else if(S.y == world.maxy - TRANSITIONEDGE)
					mirage_dir |= NORTH
				if(!mirage_dir)
					continue

				var/turf/place = locate(S.destination_x, S.destination_y, S.destination_z)
				S.AddComponent(/datum/component/mirage_border, place, mirage_dir)
