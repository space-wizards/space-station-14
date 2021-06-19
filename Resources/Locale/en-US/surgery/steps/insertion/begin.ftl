# Surgeon
surgery-step-insertion-begin-surgeon-popup = You begin to insert {PROPER($item) ->
  [false] the
  *[bucket] {""}
} {$item} into {$target}'s {$part}
surgery-step-insertion-begin-self-surgeon-popup = You begin to insert {PROPER($item) ->
  [false] the
  *[bucket] {""}
} {$item} into your {$part}
surgery-step-insertion-begin-no-zone-surgeon-popup = You begin to insert {PROPER($item) ->
  [false] the
  *[bucket] {""}
} {$item}

# Target
surgery-step-insertion-begin-target-popup = {$user} begins to insert {PROPER($item) ->
  [false] the
  *[bucket] {""}
} {$item} into your {$part}

# Outsider
surgery-step-insertion-begin-outsider-popup = {$user} begins to insert {PROPER($item) ->
  [false] the
  *[bucket] {""}
} {$item} into {$target}'s {$part}
surgery-step-insertion-begin-self-outsider-popup = {$user} begins to insert {PROPER($item) ->
  [false] the
  *[bucket] {""}
} {$item} into {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-insertion-begin-no-zone-outsider-popup = {$user} begins to insert {PROPER($item) ->
  [false] the
  *[bucket] {""}
} {$item} into {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
