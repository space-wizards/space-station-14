# This is a script to be loaded into GNU Octave.

# - Notes -
# + Be sure to check all parameters are up to date with game before use.
# + The way things are tuned, only PA level 1 is stable on Saltern.
# A singularity timestep is one second.

# - Parameters -
# It's expected that you dynamically modify these if relevant to your scenario.
global pa_particle_energy_for_level_table pa_level pa_time_between_shots
pa_particle_energy_for_level_table = [10, 30, 60, 100]
# Note that level 0 is 1 here.
pa_level = 1
pa_time_between_shots = 6

# Horizontal size (interior tiles) of mapped singulo cage
global cage_area cage_pa1 cage_pa2 cage_pa3
#  __123__
# +---+---+
cage_area = 7
cage_pa1 = 2.5
cage_pa2 = 3.5
cage_pa3 = 4.5

global energy_drain_for_level_table
energy_drain_for_level_table = [1, 2, 5, 10, 15, 20]
function retval = level_for_energy (energy)
  retval = 1
  if energy >= 1500 retval = 6; return; endif
  if energy >= 1000 retval = 5; return; endif
  if energy >= 600 retval = 4; return; endif
  if energy >= 300 retval = 3; return; endif
  if energy >= 200 retval = 2; return; endif
endfunction
function retval = radius_for_level (level)
  retval = level - 0.5
endfunction

# - Simulator -

global singulo_shot_timer
singulo_shot_timer = 0

function retval = singulo_step (energy)
  global energy_drain_for_level_table
  global pa_particle_energy_for_level_table pa_level pa_time_between_shots
  global cage_area cage_pa1 cage_pa2 cage_pa3
  global singulo_shot_timer
  level = level_for_energy(energy)
  energy_drain = energy_drain_for_level_table(level)
  energy -= energy_drain
  singulo_shot_timer += 1
  if singulo_shot_timer == pa_time_between_shots
    energy_gain_per_hit = pa_particle_energy_for_level_table(pa_level)
    # This is the bit that's complicated: the area and probability calculation.
    # Rather than try to work it out, let's do things by simply trying it.
    # This is the area of the singulo.
    singulo_area = radius_for_level(level) * 2
    # This is therefore the area in which it can move.
    effective_area = max(0, cage_area - singulo_area)
    # Assume it's at some random position within the area it can move.
    # (This is the weak point of the maths. It's not as simple as this really.)
    singulo_lpos = (rand() * effective_area)
    singulo_rpos = singulo_lpos + singulo_area
    # Check each of 3 points.
    n = 0.5
    if singulo_lpos < (cage_pa1 + n) && singulo_rpos > (cage_pa1 - n)
      energy += energy_gain_per_hit
    endif
    if singulo_lpos < (cage_pa2 + n) && singulo_rpos > (cage_pa2 - n)
      energy += energy_gain_per_hit
    endif
    if singulo_lpos < (cage_pa3 + n) && singulo_rpos > (cage_pa3 - n)
      energy += energy_gain_per_hit
    endif
    singulo_shot_timer = 0
  endif
  retval = energy
endfunction

# - Scenario -

global scenario_energy
scenario_energy = 100

function retval = scenario (x)
  global scenario_energy
  sce = scenario_energy
  scenario_energy = singulo_step(sce)
  retval = scenario_energy
endfunction

# x is in seconds.
x = 0:1:960
plot(x, arrayfun(@scenario, x))
