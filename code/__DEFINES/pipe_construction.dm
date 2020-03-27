//Construction Categories
#define PIPE_STRAIGHT			0 //2 directions: N/S, E/W
#define PIPE_BENDABLE			1 //6 directions: N/S, E/W, N/E, N/W, S/E, S/W
#define PIPE_TRINARY			2 //4 directions: N/E/S, E/S/W, S/W/N, W/N/E
#define PIPE_TRIN_M				3 //8 directions: N->S+E, S->N+E, N->S+W, S->N+W, E->W+S, W->E+S, E->W+N, W->E+N
#define PIPE_UNARY				4 //4 directions: N, S, E, W
#define PIPE_ONEDIR				5 //1 direction: N/S/E/W
#define PIPE_UNARY_FLIPPABLE	6 //8 directions: N/S/E/W/N-flipped/S-flipped/E-flipped/W-flipped

//Disposal pipe relative connection directions
#define DISP_DIR_BASE	0
#define DISP_DIR_LEFT	1
#define DISP_DIR_RIGHT	2
#define DISP_DIR_FLIP	4
#define DISP_DIR_NONE	8

//Transit tubes
#define TRANSIT_TUBE_STRAIGHT			0
#define TRANSIT_TUBE_STRAIGHT_CROSSING	1
#define TRANSIT_TUBE_CURVED				2
#define TRANSIT_TUBE_DIAGONAL			3
#define TRANSIT_TUBE_DIAGONAL_CROSSING	4
#define TRANSIT_TUBE_JUNCTION			5
#define TRANSIT_TUBE_STATION			6
#define TRANSIT_TUBE_TERMINUS			7
#define TRANSIT_TUBE_POD				8

//the open status of the transit tube station
#define STATION_TUBE_OPEN		0
#define STATION_TUBE_OPENING	1
#define STATION_TUBE_CLOSED		2
#define STATION_TUBE_CLOSING	3
