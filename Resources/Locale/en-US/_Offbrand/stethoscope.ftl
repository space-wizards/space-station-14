stethoscope-arterial-bleeding = the sound of blood rushing
stethoscope-sounds = You hear {$sounds}.

stethoscope-lung-damaged-sounds-1 = wheezing sounds
stethoscope-lung-damaged-sounds-2 = crackling sounds
stethoscope-lung-damaged-sounds-3 = gurgling sounds
stethoscope-lung-damaged-sounds-4 = grunting sounds
stethoscope-lung-damaged-sounds-5 = hissing sounds

stethoscope-lung-breathing = { $volume ->
    [shallow] shallow{" "}
   *[normal] {""}
}{ $regularity ->
    [regular] steady
   *[irregular] irregular
} { $speed ->
    [fast] faster than normal{" "}
    [faster] very rapid{" "}
    [fastest] dangerously rapid{" "}
   *[normal] {""}
}breathing

-stethoscope-heart-damaged = { $damaged ->
    [true] an irregular and
  *[false] a
}

stethoscope-heart-beating =
    .normal = { -stethoscope-heart-damaged(damaged: $damaged) } normal heartbeat
    .fast = { -stethoscope-heart-damaged(damaged: $damaged) } fast heartbeat
    .veryfast = { -stethoscope-heart-damaged(damaged: $damaged) } very fast heartbeat
    .dangerous = { -stethoscope-heart-damaged(damaged: $damaged) } very fast and faint heartbeat
