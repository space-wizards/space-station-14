// for secHUDs and medHUDs and variants. The number is the location of the image on the list hud_list
// note: if you add more HUDs, even for non-human atoms, make sure to use unique numbers for the defines!
// /datum/atom_hud expects these to be unique
// these need to be strings in order to make them associative lists
/// dead, alive, sick, health status
#define HEALTH_HUD		"1"
/// a simple line rounding the mob's number health
#define STATUS_HUD		"2"
/// the job asigned to your ID
#define ID_HUD			"3"
/// wanted, released, parroled, security status
#define WANTED_HUD		"4"
/// loyality implant
#define IMPLOYAL_HUD	"5"
/// chemical implant
#define IMPCHEM_HUD		"6"
/// tracking implant
#define IMPTRACK_HUD	"7"
/// Silicon/Mech/Circuit Status
#define DIAG_STAT_HUD	"8"
/// Silicon health bar
#define DIAG_HUD		"9"
/// Borg/Mech/Circutry power meter
#define DIAG_BATT_HUD	"10"
/// Mech health bar
#define DIAG_MECH_HUD	"11"
/// Bot HUDs
#define DIAG_BOT_HUD	"12"
/// Circuit assembly health bar
#define DIAG_CIRCUIT_HUD "13"
/// Mech/Silicon tracking beacon, Circutry long range icon
#define DIAG_TRACK_HUD	"14"
/// Airlock shock overlay
#define DIAG_AIRLOCK_HUD "15"
/// Bot path indicators
#define DIAG_PATH_HUD "16"
/// Gland indicators for abductors
#define GLAND_HUD "17"
#define SENTIENT_DISEASE_HUD	"18"
#define AI_DETECT_HUD	"19"
#define NANITE_HUD "20"
#define DIAG_NANITE_FULL_HUD "21"
/// Displays launchpads' targeting reticle
#define DIAG_LAUNCHPAD_HUD "22"
//for antag huds. these are used at the /mob level
#define ANTAG_HUD		"23"

//by default everything in the hud_list of an atom is an image
//a value in hud_list with one of these will change that behavior
#define HUD_LIST_LIST 1

//data HUD (medhud, sechud) defines
//Don't forget to update human/New() if you change these!
#define DATA_HUD_SECURITY_BASIC			1
#define DATA_HUD_SECURITY_ADVANCED		2
#define DATA_HUD_MEDICAL_BASIC			3
#define DATA_HUD_MEDICAL_ADVANCED		4
#define DATA_HUD_DIAGNOSTIC_BASIC		5
#define DATA_HUD_DIAGNOSTIC_ADVANCED	6
#define DATA_HUD_ABDUCTOR				7
#define DATA_HUD_SENTIENT_DISEASE		8
#define DATA_HUD_AI_DETECT				9

//antag HUD defines
#define ANTAG_HUD_CULT			10
#define ANTAG_HUD_REV			11
#define ANTAG_HUD_OPS			12
#define ANTAG_HUD_WIZ			13
#define ANTAG_HUD_SHADOW    	14
#define ANTAG_HUD_TRAITOR 		15
#define ANTAG_HUD_NINJA 		16
#define ANTAG_HUD_CHANGELING 	17
#define ANTAG_HUD_ABDUCTOR 		18
#define ANTAG_HUD_DEVIL			19
#define ANTAG_HUD_SINTOUCHED	20
#define ANTAG_HUD_SOULLESS		21
#define ANTAG_HUD_BROTHER		22
#define ANTAG_HUD_OBSESSED	23
#define ANTAG_HUD_FUGITIVE	24

// Notification action types
#define NOTIFY_JUMP "jump"
#define NOTIFY_ATTACK "attack"
#define NOTIFY_ORBIT "orbit"

/// cooldown for being shown the images for any particular data hud
#define ADD_HUD_TO_COOLDOWN 20
