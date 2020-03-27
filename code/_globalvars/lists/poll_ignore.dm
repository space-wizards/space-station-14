//Each lists stores ckeys for "Never for this round" option category

#define POLL_IGNORE_SENTIENCE_POTION "sentience_potion"
#define POLL_IGNORE_POSSESSED_BLADE "possessed_blade"
#define POLL_IGNORE_ALIEN_LARVA "alien_larva"
#define POLL_IGNORE_SYNDICATE "syndicate"
#define POLL_IGNORE_HOLOPARASITE "holoparasite"
#define POLL_IGNORE_POSIBRAIN "posibrain"
#define POLL_IGNORE_SPECTRAL_BLADE "spectral_blade"
#define POLL_IGNORE_CONSTRUCT "construct"
#define POLL_IGNORE_SPIDER "spider"
#define POLL_IGNORE_ASHWALKER "ashwalker"
#define POLL_IGNORE_GOLEM "golem"
#define POLL_IGNORE_SWARMER "swarmer"
#define POLL_IGNORE_DRONE "drone"
#define POLL_IGNORE_FUGITIVE "fugitive"
#define POLL_IGNORE_DEFECTIVECLONE "defective_clone"
#define POLL_IGNORE_PYROSLIME "slime"
#define POLL_IGNORE_SHADE "shade"
#define POLL_IGNORE_IMAGINARYFRIEND "imaginary_friend"
#define POLL_IGNORE_SPLITPERSONALITY "split_personality"
#define POLL_IGNORE_CONTRACTOR_SUPPORT "contractor_support"
#define POLL_IGNORE_ACADEMY_WIZARD "academy_wizard"


GLOBAL_LIST_INIT(poll_ignore_desc, list(
	POLL_IGNORE_SENTIENCE_POTION = "Sentience potion",
	POLL_IGNORE_POSSESSED_BLADE = "Possessed blade",
	POLL_IGNORE_ALIEN_LARVA = "Xenomorph larva",
	POLL_IGNORE_SYNDICATE = "Syndicate",
	POLL_IGNORE_HOLOPARASITE = "Holoparasite",
	POLL_IGNORE_POSIBRAIN = "Positronic brain",
	POLL_IGNORE_SPECTRAL_BLADE = "Spectral blade",
	POLL_IGNORE_CONSTRUCT = "Construct",
	POLL_IGNORE_SPIDER = "Spiders",
	POLL_IGNORE_ASHWALKER = "Ashwalker eggs",
	POLL_IGNORE_GOLEM = "Golems",
	POLL_IGNORE_SWARMER = "Swarmer shells",
	POLL_IGNORE_DRONE = "Drone shells",
	POLL_IGNORE_FUGITIVE = "Fugitive Hunter",
	POLL_IGNORE_DEFECTIVECLONE = "Defective clone",
	POLL_IGNORE_PYROSLIME = "Slime",
	POLL_IGNORE_SHADE = "Shade",
	POLL_IGNORE_IMAGINARYFRIEND = "Imaginary Friend",
	POLL_IGNORE_SPLITPERSONALITY = "Split Personality",
	POLL_IGNORE_CONTRACTOR_SUPPORT = "Contractor Support Unit",
	POLL_IGNORE_ACADEMY_WIZARD = "Academy Wizard Defender"
))
GLOBAL_LIST_INIT(poll_ignore, init_poll_ignore())


/proc/init_poll_ignore()
	. = list()
	for (var/k in GLOB.poll_ignore_desc)
		.[k] = list()
