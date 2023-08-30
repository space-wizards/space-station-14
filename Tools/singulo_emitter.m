# This is a script to be loaded into GNU Octave.

# - Notes -
# + Be sure to check all parameters are up to date with game before use.
# + This plots *worst-case* performance, the assumption is that it shouldn't ever randomly fail.
# + The assumption is that there is one emitter per shield point.
# + Keep in mind that to prevent the generator being destroyed, either shield must be above a limit.
#   This limit is (level*2)+1.
# The timestep used for simulation is one second.

global emitter_state emitter_timer shield_energy

emitter_state = 0
emitter_timer = 0
shield_energy = 0

function shield_clamp ()
  global shield_energy
  # ContainmentFieldConnection.SharedEnergyPool
  shield_energy = min(max(shield_energy, 0), 25)
endfunction
function shield_tick ()
  global shield_energy
  shield_energy -= 1
  shield_clamp()
endfunction
function shield_hit ()
  global shield_energy
  emitter_count = 2 # one per connection side
  receive_power = 6 # ContainmentFieldGeneratorComponent.IStartCollide.CollideWith
  power_per_connection = receive_power / 2 # ContainmentFieldGeneratorComponent.ReceivePower
  shield_energy += power_per_connection * emitter_count
  shield_clamp()
endfunction

function retval = scenario (x)
  global emitter_state emitter_timer shield_energy
  # Tick (degrade) shield
  shield_tick()
  # Timer...
  if emitter_timer > 0
    emitter_timer -= 1
  else
    # Note the logic here is written to match how EmitterComponent does it.
    # Fire first...
    shield_hit()
    # Then check if < fireBurstSize
    if emitter_state < 3
      # Then increment & reset
      emitter_state += 1
      # to fireInterval
      emitter_timer = 2
    else
      # Reset state
      emitter_state = 0
      # Worst case, fireBurstDelayMax
      emitter_timer = 10
    endif
  endif
  retval = shield_energy
endfunction

# x is in seconds.
x = 0:1:960
plot(x, arrayfun(@scenario, x))
