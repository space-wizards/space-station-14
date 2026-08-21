command-help-usage =
    Usage:
command-help-invertible =
    The behaviour of this command can be inverted using the "not" prefix.
command-description-tpto =
    Teleport the given entities to some target entity.
command-description-player-list =
    Returns a list of all player sessions.
command-description-player-self =
    Returns the current player session.
command-description-player-imm =
    Returns the session associated with the player given as argument.
command-description-player-entity =
    Returns the entities of the input sessions.
command-description-self =
    Returns the current attached entity.
command-description-physics-velocity =
    Returns the velocity of the input entities.
command-description-physics-angular-velocity =
    Returns the angular velocity of the input entities.
command-description-buildinfo =
    Provides information about the build of the game.
command-description-cmd-list =
    Returns a list of all commands, for this side.
command-description-explain =
    Explains the given expression, providing command descriptions and signatures. This only works for valid expressions, it can't explain commands that it fails to parse.
command-description-search =
    Searches through the input for the provided value.
command-description-stopwatch =
    Measures the execution time of the given expression.
command-description-types-consumers =
    Provides all commands that can consume the given type.
command-description-types-tree =
    Debug tool to return all types the command interpreter can downcast the input to.
command-description-types-gettype =
    Returns the type of the input.
command-description-types-fullname =
    Returns the full name of the input type according to CoreCLR.
command-description-as =
    Casts the input to the given type.
    Effectively a type hint if you know the type but the interpreter does not.
command-description-count =
    Counts the amount of entries in it's input, returning an integer.
command-description-map =
    Maps the input over the given block.
command-description-select =
    Selects N objects or N% of objects from the input.
    One can additionally invert this command with not to make it select everything except N objects instead.
command-description-comp =
    Returns the given component from the input entities, discarding entities without that component.
command-description-delete =
    Deletes the input entities.
command-description-ent =
    Returns the provided entity ID.
command-description-entities =
    Returns all entities on the server.
command-description-paused =
    Filters the input entities by whether or not they are paused.
command-description-with =
    Filters the input entities by whether or not they have the given component.
command-description-fuck =
    Throws an exception.
command-description-ecscomp-listty =
    Lists every type of component registered.
command-description-cd =
    Changes the session's current directory to the given relative or absolute path.
command-description-ls-here =
    Lists the contents of the current directory.
command-description-ls-in =
    Lists the contents of the given relative or absolute path.
command-description-methods-get =
    Returns all methods associated with the input type.
command-description-methods-overrides =
    Returns all methods overridden on the input type.
command-description-methods-overridesfrom =
    Returns all methods overridden from the given type on the input type.
command-description-cmd-moo =
    Asks the important questions.
command-description-cmd-descloc =
    Returns the localization string for a command's description.
command-description-cmd-getshim =
    Returns a command's execution shim.
command-description-help =
    Provides a quick rundown of how to use toolshed.
command-description-ioc-registered =
    Returns all the types registered with IoCManager on the current thread (usually the game thread)
command-description-ioc-get =
    Gets an instance of an IoC registration.
command-description-loc-tryloc =
    Tries to get a localization string, returning null if unable.
command-description-loc-loc =
    Gets a localization string, returning the unlocalized string if unable.
command-description-physics-angular_velocity =
    Returns the angular velocity of the given entities.
command-description-vars =
    Provides a list of all variables set in this session.
command-description-any =
    Returns true if there's any values in the input, otherwise false.
command-description-contains =
    Returns whether the input enumerable contains the specified value.
command-description-ArrowCommand =
    Assigns the input to a variable.
command-description-isempty =
    Returns true if the input is empty, otherwise false.
command-description-isnull =
    Returns true if the input is null, otherwise false.
command-description-unique =
    Filters the input sequence for uniqueness, removing duplicate values.
command-description-where =
    Given some input sequence IEnumerable<T>, takes a block of signature T -> bool that decides if each input value should be included in the output sequence.
command-description-do =
    Backwards compatibility with BQL, applies the given old commands over the input sequence.
command-description-named =
    Filters the input entities by their name, with the regex ^selector$.
command-description-prototyped =
    Filters the input entities by their prototype.
command-description-nearby =
    Creates a new list of all entities nearby the inputs within the given range.
command-description-first =
    Returns the first entry of the given enumerable.
command-description-splat =
    "Splats" a block, value, or variable, creating N copies of it in a list.
command-description-val =
    Casts the given value, block, or variable to the given type. This is mostly a workaround for current limitations of variables.
command-description-var =
    Returns the contents of the given variable. This will attempt to automatically infer a variables type. Compound commands that modify a variable may need to use the 'val' command instead.
command-description-actor-controlled =
    Filters entities by whether or not they're actively controlled.
command-description-actor-session =
    Returns the sessions associated with the input entities.
command-description-physics-parent =
    Returns the parent(s) of the input entities.
command-description-emplace =
    Runs the given block over it's inputs, with the input value placed into the variable $value within the block.
    Additionally breaks out $wx, $wy, $proto, $desc, $name, and $paused for entities.
    Can also have breakout values for other types, consult the documentation for that type for further info.
command-description-AddCommand =
    Performs numeric addition.
command-description-SubtractCommand =
    Performs numeric subtraction.
command-description-MultiplyCommand =
    Performs numeric multiplication.
command-description-DivideCommand =
    Performs numeric division.
command-description-min =
    Returns the minimum of two values.
command-description-max =
    Returns the maximum of two values.
command-description-BitAndCommand =
    Performs bitwise AND.
command-description-bitor =
    Performs bitwise OR.
command-description-BitXorCommand =
    Performs bitwise XOR.
command-description-neg =
    Negates the input.
command-description-GreaterThanCommand =
    Performs a greater-than comparison, x > y.
command-description-LessThanCommand =
    Performs a less-than comparison, x < y.
command-description-GreaterThanOrEqualCommand =
    Performs a greater-than-or-equal comparison, x >= y.
command-description-LessThanOrEqualCommand =
    Performs a less-than-or-equal comparison, x <= y.
command-description-EqualCommand =
    Performs an equality comparison, returning true if the inputs are equal.
command-description-NotEqualCommand =
    Performs an equality comparison, returning true if the inputs are not equal.
command-description-append =
    Appends a value to the input enumerable.
command-description-DefaultIfNullCommand =
    Replaces the input with the type's default value if it is null, albeit only for value types (not objects).
command-description-OrValueCommand =
    If the input is null, uses the provided alternate value.
command-description-DebugPrintCommand =
    Prints the given value transparently, for debug prints in a command run.
command-description-i =
    Integer constant.
command-description-f =
    Float constant.
command-description-s =
    String constant.
command-description-b =
    Bool constant.
command-description-join =
    Joins two sequences together into one sequence.
command-description-reduce =
    Given a block to use as a reducer, turns a sequence into a single value.
    The left hand side of the block is implied, and the right hand is stored in $value.
command-description-rep =
    Repeats the input value N times to form a sequence.
command-description-take =
    Takes N values from the input sequence
command-description-spawn-at =
    Spawns an entity at the given coordinates.
command-description-spawn-on =
    Spawns an entity on the given entity, at it's coordinates.
command-description-spawn-in =
    Spawns an entity in the given container on the given entity, dropping it at its coordinates if it doesn't fit
command-description-spawn-attached =
    Spawns an entity attached to the given entity, at (0 0) relative to it.
command-description-mappos =
    Returns an entity's coordinates relative to it's current map.
command-description-pos =
    Returns an entity's coordinates.
command-description-tp-coords =
    Teleports the given entities to the target coordinates.
command-description-tp-to =
    Teleports the given entities to the target entity.
command-description-tp-into =
    Teleports the given entities "into" the target entity, attaching it at (0 0) relative to it.
command-description-comp-get =
    Gets the given component from the given entity.
command-description-comp-add =
    Adds the given component to the given entity.
command-description-comp-ensure =
    Ensures the given entity has the given component.
command-description-comp-has =
    Check if the given entity has the given component.
command-description-AddVecCommand =
    Adds a scalar (single value) to every element in the input.
command-description-SubVecCommand =
    Subtracts a scalar (single value) from every element in the input.
command-description-MulVecCommand =
    Multiplies a scalar (single value) by every element in the input.
command-description-DivVecCommand =
    Divides every element in the input by a scalar (single value).
command-description-rng-to =
    Returns a number between the input (inclusive) and the argument (exclusive).
command-description-rng-from =
    Returns a number between the argument (inclusive) and the input (exclusive))
command-description-rng-prob =
    Returns a boolean based on the input probability/chance (from 0 to 1)
command-description-sum =
    Computes the sum of the input.
command-description-bin =
    "Bins" the input, counting up how many times each unique element occurs.
command-description-extremes =
    Returns the two extreme ends of a list, interwoven.
command-description-sortby =
    Sorts the input least to greatest by the computed key.
command-description-sortmapby =
    Sorts the input least to greatest by the computed key, replacing the value with it's computed key afterward.
command-description-sort =
    Sorts the input least to greatest.
command-description-sortdownby =
    Sorts the input greatest to least by the computed key.
command-description-sortmapdownby =
    Sorts the input greatest to least by the computed key, replacing the value with it's computed key afterward.
command-description-sortdown =
    Sorts the input greatest to least.
command-description-iota =
    Returns a list of numbers 1 to N.
command-description-to =
    Returns a list of numbers N to M.
command-description-curtick =
    The current game tick.
command-description-curtime =
    The current game time (a TimeSpan)
command-description-realtime =
    The current realtime since startup (a TimeSpan)
command-description-servertime =
    The current server game time, or zero if we are the server (a TimeSpan)
command-description-replace =
    Replaces the input entities with the given prototype, preserving position and rotation (but nothing else)
command-description-allcomps =
    Returns all components on the given entity.
command-description-entitysystemupdateorder-tick =
    Lists the tick update order of entity systems.
command-description-entitysystemupdateorder-frame =
    Lists the frame update order of entity systems.
command-description-more =
    Prints the contents of $more, i.e. any extras that Toolshed didn't print from the last command.
command-description-ModulusCommand =
    Computes the modulus of two values.
    This is usually remainder, check C#'s documentation for the type.
command-description-ModVecCommand =
    Performs the modulus operation over the input with the given constant right-hand value.
command-description-BitAndNotCommand =
    Performs bitwise AND-NOT over the input.
command-description-bitornot =
    Performs bitwise OR-NOT over the input.
command-description-BitXnorCommand =
    Performs bitwise XNOR over the input.
command-description-BitNotCommand =
    Performs bitwise NOT on the input.
command-description-abs =
    Computes the absolute value of the input (removing the sign)
command-description-average =
    Computes the average (arithmetic mean) of the input.
command-description-bibytecount =
    Returns the size of the input in bytes, given that the input implements IBinaryInteger.
    This is NOT sizeof.
command-description-shortestbitlength =
    Returns the minimum number of bits needed to represent the input value.
command-description-countleadzeros =
    Counts the number of leading binary zeros in the input value.
command-description-counttrailingzeros =
    Counts the number of trailing binary zeros in the input value.
command-description-fpi =
    pi (3.14159...) as a float.
command-description-fe =
    e (2.71828...) as a float.
command-description-ftau =
    tau (6.28318...) as a float.
command-description-fepsilon =
    The epsilon value for a float, exactly 1.4e-45.
command-description-dpi =
    pi (3.14159...) as a double.
command-description-de =
    e (2.71828...) as a double.
command-description-dtau =
    tau (6.28318...) as a double.
command-description-depsilon =
    The epsilon value for a double, exactly 4.9406564584124654E-324.
command-description-hpi =
    pi (3.14...) as a half.
command-description-he =
    e (2.71...) as a half.
command-description-htau =
    tau (6.28...) as a half.
command-description-hepsilon =
    The epsilon value for a half, exactly 5.9604645E-08.
command-description-floor =
    Returns the floor of the input value (rounding toward zero).
command-description-ceil =
    Returns the ceil of the input value (rounding away from zero).
command-description-round =
    Rounds the input value.
command-description-trunc =
    Truncates the input value.
command-description-round2frac =
    Rounds the input value to the specified number of fractional digits.
command-description-exponentbytecount =
    Returns the number of bytes required to store the exponent.
command-description-significandbytecount =
    Returns the number of bytes required to store the significand.
command-description-significandbitcount =
    Returns the exact bit length of the significand.
command-description-exponentshortestbitcount =
    Returns the minimum number of bits to store the exponent.
command-description-stepnext =
    Steps to the next float value, adding one to the significand with carry.
command-description-stepprev =
    Steps to the previous float value, subtracting one from the significand with carry.
command-description-checkedto =
    Converts from the input numeric type to the target, erroring if not possible.
command-description-saturateto =
    Converts from the input numeric type to the target, saturating if the value is out of range.
    For example, converting 382 to a byte would saturate to 255 (the maximum value of a byte).
command-description-truncto =
    Converts from the input numeric type to the target, with truncation.
    In the case of integers, this is a bit cast with sign extension.
command-description-iscanonical =
    Returns whether the input is in canonical form.
command-description-iscomplex =
    Returns whether the input is a complex number (by value, not by type)
command-description-iseven =
    Returns whether the input is even.
    Not a javascript package.
command-description-isodd =
    Returns whether the input is odd.
command-description-isfinite =
    Returns whether the input is finite.
command-description-isimaginary =
    Returns whether the input is purely imaginary (no real part).
command-description-isinfinite =
    Returns whether the input is infinite.
command-description-isinteger =
    Returns whether the input is an integer (by value, not by type)
command-description-isnan =
    Returns whether the input is Not a Number (NaN).
    This is a special floating point value, so this is by value, not by type.
command-description-isnegative =
    Returns whether the input is negative.
command-description-ispositive =
    Returns whether the input is positive.
command-description-isreal =
    Returns whether the input is purely real (no imaginary part).
command-description-issubnormal =
    Returns whether the input is in sub-normal form.
command-description-iszero =
    Returns whether the input is zero.
command-description-pow =
    Computes the power of its lefthand to its righthand. x^y.
command-description-sqrt =
    Computes the square root of its input.
command-description-cbrt =
    Computes the cube root of its input.
command-description-root =
    Computes the Nth root of its input.
command-description-hypot =
    Computes the hypotenuse of a triangle with the given sides A and B.
command-description-sin =
    Computes the sine of the input.
command-description-sinpi =
    Computes the sine of the input multiplied by pi.
command-description-asin =
    Computes the arcsine of the input.
command-description-asinpi =
    Computes the arcsine of the input multiplied by pi.
command-description-cos =
    Computes the cosine of the input.
command-description-cospi =
    Computes the cosine of the input multiplied by pi.
command-description-acos =
    Computes the arcosine of the input.
command-description-acospi =
    Computes the arcosine of the input multiplied by pi.
command-description-tan =
    Computes the tangent of the input.
command-description-tanpi =
    Computes the tangent of the input multiplied by pi.
command-description-atan =
    Computes the arctangent of the input.
command-description-atanpi =
    Computes the arctangent of the input multiplied by pi.
command-description-iterate =
    Iterates the given function over the input N times, returning a list of results.
    Think of this like successively applying the function to a value, tracking all the intermediate values.
command-description-pick =
    Picks a random value from the input.
command-description-tee =
    Tees the input into the given block, ignoring the block's result.
    This essentially lets you have a branch in your code to do multiple operations on one value.
command-description-cmd-info =
    Returns a CommandSpec for the given command.
    On its own, this means it'll print the command's help message.
command-description-comp-rm =
    Removes the given component from the entity.

command-description-overlay-toggle = Toggle an overlay on or off
command-description-overlay-add = Add an overlay (if it does not already exist)
command-description-overlay-remove = Remove an overlay
