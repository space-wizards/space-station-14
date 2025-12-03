#!/usr/bin/env python3

import argparse
import json
import os
from PIL import Image
from glob import iglob
from jsonschema import Draft7Validator, ValidationError
from typing import Any, List, Optional

ALLOWED_RSI_DIR_GARBAGE = {
    "meta.json",
    ".DS_Store",
    "thumbs.db",
    ".directory"
}

errors: List["RsiError"] = []

def main() -> int:
    parser = argparse.ArgumentParser("validate_rsis.py", description="Validates RSI file integrity for mistakes the engine does not catch while loading.")
    parser.add_argument("directories", nargs="+", help="Directories to look for RSIs in")

    args = parser.parse_args()
    schema = load_schema()

    for dir in args.directories:
        check_dir(dir, schema)

    for error in errors:
        print(f"{error.path}: {error.message}")

    return 1 if errors else 0


def check_dir(dir: str, schema: Draft7Validator):
    for rsi_rel in iglob("**/*.rsi", root_dir=dir, recursive=True):
        rsi_path = os.path.join(dir, rsi_rel)
        try:
            check_rsi(rsi_path, schema)
        except Exception as e:
            add_error(rsi_path, f"Failed to validate RSI (script bug): {e}")


def check_rsi(rsi: str, schema: Draft7Validator):
    meta_path = os.path.join(rsi, "meta.json")

    # Try to load meta.json
    try:
        meta_json = read_json(meta_path)
    except Exception as e:
        add_error(rsi, f"Failed to read meta.json: {e}")
        return

    # Check if meta.json passes schema.
    schema_errors: List[ValidationError] = list(schema.iter_errors(meta_json))
    if schema_errors:
        for error in schema_errors:
            add_error(rsi, f"meta.json: [{error.json_path}] {error.message}")
        # meta.json may be corrupt, can't safely proceed.
        return

    state_names = {state["name"] for state in meta_json["states"]}

    # Go over contents of RSI directory and ensure there is no extra garbage.
    for name in os.listdir(rsi):
        if name in ALLOWED_RSI_DIR_GARBAGE:
            continue

        if not name.endswith(".png"):
            add_error(rsi, f"Illegal file inside RSI: {name}")
            continue

        # All PNGs must be defined in the meta.json
        png_state_name = name[:-4]
        if png_state_name not in state_names:
            add_error(rsi, f"PNG not defined in metadata: {name}")


    # Validate state delays.
    for state in meta_json["states"]:
        state_name: str = state["name"]

        # Validate state delays.
        delays: Optional[List[List[float]]] = state.get("delays")
        if not delays:
            continue

        # Validate directions count in metadata and delays count matches.
        directions: int = state.get("directions", 1)
        if directions != len(delays):
            add_error(rsi, f"{state_name}: direction count ({directions}) doesn't match delay set specified ({len(delays)})")
            continue

        # Validate that each direction array has the same length.
        lengths: List[float] = []
        for dir in delays:
            # Robust rounds to millisecond precision.
            lengths.append(round(sum(dir), 3))

        if any(l != lengths[0] for l in lengths):
            add_error(rsi, f"{state_name}: mismatching total durations between state directions: {', '.join(map(str, lengths))}")

    frame_width = meta_json["size"]["x"]
    frame_height = meta_json["size"]["y"]

    # Validate state PNGs.
    # We only check they're the correct size and that they actually exist and load.
    for state in meta_json["states"]:
        state_name: str = state["name"]

        png_name = os.path.join(rsi, f"{state_name}.png")
        try:
            image = Image.open(png_name)
        except Exception as e:
            add_error(rsi, f"{state_name}: failed to open state {state_name}.png")
            continue

        # Check that size is a multiple of the metadata frame size.
        size = image.size
        if size[0] % frame_width != 0 or size[1] % frame_height != 0:
            add_error(rsi, f"{state_name}: sprite sheet of {size[0]}x{size[1]} is not size multiple of RSI size ({frame_width}x{frame_height}).png")
            continue

        # Check that the sprite sheet is big enough to possibly fit all the frames listed in metadata.
        frames_w = size[0] // frame_width
        frames_h = size[1] // frame_height

        directions: int = state.get("directions", 1)
        delays: Optional[List[List[float]]] = state.get("delays", [[1]] * directions)
        frame_count = sum(map(len, delays))
        max_sheet_frames = frames_w * frames_h

        if frame_count > max_sheet_frames:
            add_error(rsi, f"{state_name}: sprite sheet of {size[0]}x{size[1]} is too small, metadata defines {frame_count} frames, but it can only fit {max_sheet_frames} at most")
            continue

    # Check if state name exists
    for state in meta_json["states"]:
        state_name: str = state["name"]
        if state_name == "":
            add_error(rsi, f"state name cannot be an empty string.")
            return

    # We're good!
    return


def load_schema() -> Draft7Validator:
    base_path = os.path.dirname(os.path.realpath(__file__))
    schema_path = os.path.join(base_path, "rsi.json")
    schema_json = read_json(schema_path)

    return Draft7Validator(schema_json)


def read_json(path: str) -> Any:
    with open(path, "r", encoding="utf-8-sig") as f:
        return json.load(f)


def add_error(rsi: str, message: str):
    errors.append(RsiError(rsi, message))


class RsiError:
    def __init__(self, path: str, message: str):
        self.path = path
        self.message = message


exit(main())
