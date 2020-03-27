#define EMBEDID "embed-[embed_chance]-[embedded_fall_chance]-[embedded_pain_chance]-[embedded_pain_multiplier]-[embedded_fall_pain_multiplier]-[embedded_impact_pain_multiplier]-[embedded_unsafe_removal_pain_multiplier]-[embedded_unsafe_removal_time]-[embedded_ignore_throwspeed_threshold]"

/proc/getEmbeddingBehavior(embed_chance = EMBED_CHANCE,
                  embedded_fall_chance = EMBEDDED_ITEM_FALLOUT,
                  embedded_pain_chance = EMBEDDED_PAIN_CHANCE,
                  embedded_pain_multiplier = EMBEDDED_PAIN_MULTIPLIER,
                  embedded_fall_pain_multiplier = EMBEDDED_FALL_PAIN_MULTIPLIER,
                  embedded_impact_pain_multiplier = EMBEDDED_IMPACT_PAIN_MULTIPLIER,
                  embedded_unsafe_removal_pain_multiplier = EMBEDDED_UNSAFE_REMOVAL_PAIN_MULTIPLIER,
                  embedded_unsafe_removal_time = EMBEDDED_UNSAFE_REMOVAL_TIME,
                  embedded_ignore_throwspeed_threshold = FALSE)
  . = locate(EMBEDID)
  if (!.)
    . = new /datum/embedding_behavior(embed_chance, embedded_fall_chance, embedded_pain_chance, embedded_pain_multiplier, embedded_fall_pain_multiplier, embedded_impact_pain_multiplier, embedded_unsafe_removal_pain_multiplier, embedded_unsafe_removal_time, embedded_ignore_throwspeed_threshold)

/datum/embedding_behavior
  var/embed_chance
  var/embedded_fall_chance
  var/embedded_pain_chance
  var/embedded_pain_multiplier //The coefficient of multiplication for the damage this item does while embedded (this*w_class)
  var/embedded_fall_pain_multiplier //The coefficient of multiplication for the damage this item does when falling out of a limb (this*w_class)
  var/embedded_impact_pain_multiplier //The coefficient of multiplication for the damage this item does when first embedded (this*w_class)
  var/embedded_unsafe_removal_pain_multiplier //The coefficient of multiplication for the damage removing this without surgery causes (this*w_class)
  var/embedded_unsafe_removal_time //A time in ticks, multiplied by the w_class.
  var/embedded_ignore_throwspeed_threshold //if we don't give a damn about EMBED_THROWSPEED_THRESHOLD

/datum/embedding_behavior/New(embed_chance = EMBED_CHANCE,
                  embedded_fall_chance = EMBEDDED_ITEM_FALLOUT,
                  embedded_pain_chance = EMBEDDED_PAIN_CHANCE,
                  embedded_pain_multiplier = EMBEDDED_PAIN_MULTIPLIER,
                  embedded_fall_pain_multiplier = EMBEDDED_FALL_PAIN_MULTIPLIER,
                  embedded_impact_pain_multiplier = EMBEDDED_IMPACT_PAIN_MULTIPLIER,
                  embedded_unsafe_removal_pain_multiplier = EMBEDDED_UNSAFE_REMOVAL_PAIN_MULTIPLIER,
                  embedded_unsafe_removal_time = EMBEDDED_UNSAFE_REMOVAL_TIME,
                  embedded_ignore_throwspeed_threshold = FALSE)
  src.embed_chance = embed_chance
  src.embedded_fall_chance = embedded_fall_chance
  src.embedded_pain_chance = embedded_pain_chance
  src.embedded_pain_multiplier = embedded_pain_multiplier
  src.embedded_fall_pain_multiplier = embedded_fall_pain_multiplier
  src.embedded_impact_pain_multiplier = embedded_impact_pain_multiplier
  src.embedded_unsafe_removal_pain_multiplier = embedded_unsafe_removal_pain_multiplier
  src.embedded_unsafe_removal_time = embedded_unsafe_removal_time
  src.embedded_ignore_throwspeed_threshold = embedded_ignore_throwspeed_threshold
  tag = EMBEDID

/datum/embedding_behavior/proc/setRating(embed_chance, embedded_fall_chance, embedded_pain_chance, embedded_pain_multiplier, embedded_fall_pain_multiplier, embedded_impact_pain_multiplier, embedded_unsafe_removal_pain_multiplier, embedded_unsafe_removal_time, embedded_ignore_throwspeed_threshold)
  return getEmbeddingBehavior((isnull(embed_chance) ? src.embed_chance : embed_chance),\
                  (isnull(embedded_fall_chance) ? src.embedded_fall_chance : embedded_fall_chance),\
                  (isnull(embedded_pain_chance) ? src.embedded_pain_chance : embedded_pain_chance),\
                  (isnull(embedded_pain_multiplier) ? src.embedded_pain_multiplier : embedded_pain_multiplier),\
                  (isnull(embedded_fall_pain_multiplier) ? src.embedded_fall_pain_multiplier : embedded_fall_pain_multiplier),\
                  (isnull(embedded_impact_pain_multiplier) ? src.embedded_impact_pain_multiplier : embedded_impact_pain_multiplier),\
                  (isnull(embedded_unsafe_removal_pain_multiplier) ? src.embedded_unsafe_removal_pain_multiplier : embedded_unsafe_removal_pain_multiplier),\
                  (isnull(embedded_unsafe_removal_time) ? src.embedded_unsafe_removal_time : embedded_unsafe_removal_time),\
                  (isnull(embedded_ignore_throwspeed_threshold) ? src.embedded_ignore_throwspeed_threshold : embedded_ignore_throwspeed_threshold))

#undef EMBEDID
