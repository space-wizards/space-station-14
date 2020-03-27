#define SEE_INVISIBLE_MINIMUM 5

#define INVISIBILITY_LIGHTING 20

#define SEE_INVISIBLE_LIVING 25

//#define SEE_INVISIBLE_LEVEL_ONE 35 //currently unused
//#define INVISIBILITY_LEVEL_ONE 35 //currently unused

//#define SEE_INVISIBLE_LEVEL_TWO 45 //currently unused
//#define INVISIBILITY_LEVEL_TWO 45 //currently unused

#define INVISIBILITY_OBSERVER 60
#define SEE_INVISIBLE_OBSERVER 60

#define INVISIBILITY_MAXIMUM 100 //the maximum allowed for "real" objects

#define INVISIBILITY_ABSTRACT 101 //only used for abstract objects (e.g. spacevine_controller), things that are not really there.

#define BORGMESON		(1<<0)
#define BORGTHERM		(1<<1)
#define BORGXRAY 		(1<<2)
#define BORGMATERIAL	(1<<3)

//for clothing visor toggles, these determine which vars to toggle
#define VISOR_FLASHPROTECT	(1<<0)
#define VISOR_TINT			(1<<1)
#define VISOR_VISIONFLAGS	(1<<2) //all following flags only matter for glasses
#define VISOR_DARKNESSVIEW	(1<<3)
#define VISOR_INVISVIEW		(1<<4)

//for whether AI eyes see static, and whether it is mouse-opaque or not
#define USE_STATIC_NONE			0
#define USE_STATIC_TRANSPARENT	1
#define USE_STATIC_OPAQUE		2
