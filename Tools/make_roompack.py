#!/usr/bin/python
# Analyze the rectangular bounding boxes in a greyscale bitmap to create
# dungeon room pack configs.

import argparse
import cv2
from dataclasses import dataclass


SUBDIVISIONS = 128
MIN_VALUE = 1
MAX_VALUE = 256

assert(MAX_VALUE % SUBDIVISIONS == 0)

@dataclass
class Box2:
    left: int
    bottom: int
    right: int
    top: int

@dataclass
class RoomPackBitmap:
    width: int
    height: int
    rooms: list


def analyze_bitmap(fname, centered = False, offset_x = 0, offset_y = 0):
    image = cv2.imread(fname, cv2.IMREAD_GRAYSCALE)

    contours = []

    for i in range(0, 1 + SUBDIVISIONS):
        lower = MAX_VALUE / SUBDIVISIONS * (i - 1)
        upper = MAX_VALUE / SUBDIVISIONS *  i - 1

        lower = max(MIN_VALUE, lower)
        upper = min(MAX_VALUE - 1, upper)

        image_slice = cv2.inRange(image, lower, upper)
        image_mask = cv2.threshold(image_slice, 0, 255, cv2.THRESH_TOZERO)[1]
        new_contours = cv2.findContours(image_mask, cv2.RETR_LIST, cv2.CHAIN_APPROX_SIMPLE)

        if len(new_contours[0]) == 0:
            continue

        contours += new_contours[0:-1]

    image_height = len(image)
    image_width = len(image[0])
    rooms = []

    if centered:
        offset_x -= image_width // 2
        offset_y -= image_height // 2

    for contour in contours:
        for subcontour in contour:
            x, y, w, h = cv2.boundingRect(subcontour)

            box = Box2(offset_x + x,
                       offset_y + y,
                       offset_x + x + w,
                       offset_y + y + h)

            rooms.append(box)

    return RoomPackBitmap(image_width, image_height, rooms)


def main():
    parser = argparse.ArgumentParser(description='Calculate rooms from a greyscale bitmap')

    parser.add_argument('file', type=str,
                        help='a greyscale bitmap')

    parser.add_argument('--center', action=argparse.BooleanOptionalAction,
                        default=False,
                        help='center the output coordinates')

    parser.add_argument('--offset', type=int,
                        nargs=2,
                        default=[0, 0],
                        help='offset the output coordinates')

    args = parser.parse_args()

    result = analyze_bitmap(args.file, args.center, args.offset[0], args.offset[1])


    print(f"  size: {result.width},{result.height}")
    print("  rooms:")

    for room in result.rooms:
        print(f"    - {room.left},{room.bottom},{room.right},{room.top}")

    print("")
    print(f"Generated {len(result.rooms)} rooms.")

if __name__ == "__main__":
    main()

