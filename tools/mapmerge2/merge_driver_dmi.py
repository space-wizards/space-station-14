#!/usr/bin/env python3
import sys
import dmi

def images_equal(left, right):
    if left.size != right.size:
        return False
    w, h = left.size
    left_load, right_load = left.load(), right.load()
    for y in range(0, h):
        for x in range(0, w):
            lpixel, rpixel = left_load[x, y], right_load[x, y]
            # quietly ignore changes where both pixels are fully transparent
            if lpixel != rpixel and (lpixel[3] != 0 or rpixel[3] != 0):
                return False
    return True

def states_equal(left, right):
    result = True

    # basic properties
    for attr in ('loop', 'rewind', 'movement', 'dirs', 'delays', 'hotspots', 'framecount'):
        lval, rval = getattr(left, attr), getattr(right, attr)
        if lval != rval:
            result = False

    # frames
    for (left_frame, right_frame) in zip(left.frames, right.frames):
        if not images_equal(left_frame, right_frame):
            result = False

    return result

def key_of(state):
    return (state.name, state.movement)

def dictify(sheet):
    result = {}
    for state in sheet.states:
        k = key_of(state)
        if k in result:
            print(f"    duplicate {k!r}")
        result[k] = state
    return result

def three_way_merge(base, left, right):
    base_dims = base.width, base.height
    if base_dims != (left.width, left.height) or base_dims != (right.width, right.height):
        print("Dimensions have changed:")
        print(f"    Base: {base.width} x {base.height}")
        print(f"    Ours: {left.width} x {left.height}")
        print(f"    Theirs: {right.width} x {right.height}")
        return True, None

    base_states, left_states, right_states = dictify(base), dictify(left), dictify(right)

    new_left = {k: v for k, v in left_states.items() if k not in base_states}
    new_right = {k: v for k, v in right_states.items() if k not in base_states}
    new_both = {}
    conflicts = []
    for key, state in list(new_left.items()):
        in_right = new_right.get(key, None)
        if in_right:
            if states_equal(state, in_right):
                # allow it
                new_both[key] = state
            else:
                # generate conflict states
                print(f" C: {state.name!r}: added differently in both!")
                state.name = f"{state.name} !CONFLICT! left"
                conflicts.append(state)
                in_right.name = f"{state.name} !CONFLICT! right"
                conflicts.append(in_right)
            # don't add it a second time
            del new_left[key]
            del new_right[key]

    final_states = []
    # add states that are currently in the base
    for state in base.states:
        in_left = left_states.get(key_of(state), None)
        in_right = right_states.get(key_of(state), None)
        left_equals = in_left and states_equal(state, in_left)
        right_equals = in_right and states_equal(state, in_right)

        if not in_left and not in_right:
            # deleted in both left and right, it's just deleted
            print(f"    {state.name!r}: deleted in both")
        elif not in_left:
            # left deletes
            print(f"    {state.name!r}: deleted in left")
            if not right_equals:
                print(f"    ... but modified in right")
                final_states.append(in_right)
        elif not in_right:
            # right deletes
            print(f"    {state.name!r}: deleted in right")
            if not left_equals:
                print(f"    ... but modified in left")
                final_states.append(in_left)
        elif left_equals and right_equals:
            # changed in neither
            #print(f"Same in both: {state.name!r}")
            final_states.append(state)
        elif left_equals:
            # changed only in right
            print(f"    {state.name!r}: changed in left")
            final_states.append(in_right)
        elif right_equals:
            # changed only in left
            print(f"    {state.name!r}: changed in right")
            final_states.append(in_left)
        elif states_equal(in_left, in_right):
            # changed in both, to the same thing
            print(f"    {state.name!r}: changed same in both")
            final_states.append(in_left)  # either or
        else:
            # changed in both
            name = state.name
            print(f" C: {name!r}: changed differently in both!")
            state.name = f"{name} !CONFLICT! base"
            conflicts.append(state)
            in_left.name = f"{name} !CONFLICT! left"
            conflicts.append(in_left)
            in_right.name = f"{name} !CONFLICT! right"
            conflicts.append(in_right)

    # add states which both left and right added the same
    for key, state in new_both.items():
        print(f"    {state.name!r}: added same in both")
        final_states.append(state)

    # add states that are brand-new in the left
    for key, state in new_left.items():
        print(f"    {state.name!r}: added in left")
        final_states.append(state)

    # add states that are brand-new in the right
    for key, state in new_right.items():
        print(f"    {state.name!r}: added in right")
        final_states.append(state)

    final_states.extend(conflicts)
    merged = dmi.Dmi(base.width, base.height)
    merged.states = final_states
    return len(conflicts), merged

def main(path, original, left, right):
    print(f"Merging icon: {path}")

    icon_orig = dmi.Dmi.from_file(original)
    icon_left = dmi.Dmi.from_file(left)
    icon_right = dmi.Dmi.from_file(right)

    trouble, merged = three_way_merge(icon_orig, icon_left, icon_right)
    if merged:
        merged.to_file(left)
    if trouble:
        print("!!! Manual merge required!")
        if merged:
            print("    A best-effort merge was performed. You must edit the icon and remove all")
            print("    icon states marked with !CONFLICT!, leaving only the desired icon.")
        else:
            print("    The icon was totally unable to be merged, you must start with one version")
            print("    or the other and manually resolve the conflict.")
        print("    Information about which states conflicted is listed above.")
    return trouble

if __name__ == '__main__':
    if len(sys.argv) != 6:
        print("DMI merge driver called with wrong number of arguments")
        print("    usage: merge-driver-dmi %P %O %A %B %L")
        exit(1)

    # "left" is also the file that ought to be overwritten
    _, path, original, left, right, conflict_size_marker = sys.argv
    exit(main(path, original, left, right))
