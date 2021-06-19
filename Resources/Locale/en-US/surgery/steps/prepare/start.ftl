# Surgeon
surgery-prepare-start-surgeon-popup = You drape {$item} over {$target}'s {$zone} to prepare for a {$procedure}
surgery-prepare-start-no-zone-surgeon-popup = You drape {$item} over {$target} to prepare for a {$procedure}
surgery-prepare-start-self-surgeon-popup = You drape {$item} over your {$zone} to prepare for a {$procedure}
surgery-prepare-start-self-no-zone-surgeon-popup = You drape {$item} over yourself to prepare for a {$procedure}

# Target
surgery-prepare-start-target-popup = {$user} drapes {$item} over your {$zone} to prepare for a surgery
surgery-prepare-start-no-zone-target-popup = {$user} drapes {$item} over you to prepare for a surgery

# Outsider
surgery-prepare-start-outsider-popup = {$user} drapes {$item} over {$target}'s {$zone} to prepare for surgery
surgery-prepare-start-no-zone-outsider-popup = {$user} drapes {$item} over {$target} to prepare for surgery
surgery-prepare-start-self-outsider-popup = {$user} drapes {$item} over {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part} to prepare for a {$procedure}
surgery-prepare-start-self-no-zone-outsider-popup = {$user} drapes {$item} over {GENDER($user) ->
  [male] himself
  [female] herself
  *[other] themself
} to prepare for a surgery
