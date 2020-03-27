import os
import sys
import inspect
import shutil

def AddToPath(path):
  if path not in sys.path:
    sys.path.insert(0, path)
    delimeter = ':' if os.name == "posix" else ";"
    os.environ['PATH'] = path + delimeter + os.environ['PATH']

current_dir = os.path.split(inspect.getfile(inspect.currentframe()))[0]

AddToPath(os.path.abspath(os.path.join(current_dir, "third_party/Imaging-1.1.7/PIL")))
AddToPath(os.path.abspath(os.path.join(current_dir, "third_party/zlib")))

import Image
import _imaging

def PngSave(im, file):
  # From http://blog.client9.com/2007/08/28/python-pil-and-png-metadata-take-2.html

  # these can be automatically added to Image.info dict
  # they are not user-added metadata
  reserved = ('interlace', 'gamma', 'dpi', 'transparency', 'aspect')

  # undocumented class
  import PngImagePlugin
  meta = PngImagePlugin.PngInfo()

  # copy metadata into new object
  for k,v in im.info.iteritems():
      if k in reserved: continue
      meta.add_text(k, v, 0)

  # and save
  im.save(file, "PNG", pnginfo=meta)

def ProcessFile(path):
  name, ext = os.path.splitext(path)
  ext = ext.lower()
  if (ext != ".dmi" and ext != ".png") or os.path.splitext(name)[1] == ".new":
    return

  try:
    im = Image.open(path)
    print name + ": " + im.format, im.size, im.mode
    if im.mode != "RGBA":
      return
    width, height = im.size
    pix = im.load()

    n_transparent = 0

    make_opaque = []

    def add(x, y):
      if pix[x, y][3] == 0:
        make_opaque.append((x, y))

    for x in range(0, width):
      for y in range(0, height):
        if pix[x, y][3] > 0:
          if x > 0:
            add(x - 1, y)
          if x < width - 1:
            add(x + 1, y)
          if y > 0:
            add(x, y - 1)
          if y < height - 1:
            add(x, y + 1)
        else:
          n_transparent += 1

    for coords in make_opaque:
      pix[coords] = (0, 0, 0, 1)

    PngSave(im, path)
  except:
    print "Could not process " + name

root_dir = os.path.abspath(os.path.join(current_dir, "../../"))
icons_dir = os.path.join(root_dir, "icons")

def Main():
  if len(sys.argv) != 2:
    print "Usage: hitbox_expander.py filename.dmi"
    return 0

  try:
    with open(sys.argv[1]):
      ProcessFile(os.path.abspath(sys.argv[1]))
      return 0
  except IOError:
    pass

  for root, subdirs, files in os.walk(icons_dir):
    for file in files:
      if file == sys.argv[1]:
        path = os.path.join(root, file)
        ProcessFile(path)
        return 0

  print "File not found: " + sys.argv[1]

if __name__ == "__main__":
  Main()
