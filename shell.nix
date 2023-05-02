{ pkgs ? import <nixpkgs> {} }:

let
  dependencies = with pkgs; [
    dotnetCorePackages.sdk_7_0
    glfw
    SDL2
    glibc
    libGL
    openal
    freetype
    fluidsynth
    gtk3
    pango
    cairo
    atk
    zlib
    glib
    gdk-pixbuf
  ];
in pkgs.mkShell {
  name = "space-station-14-devshell";
  buildInputs = [ pkgs.gtk3 ];
  inputsFrom = dependencies;
  shellHook = ''
    export XDG_DATA_DIRS=$GSETTINGS_SCHEMAS_PATH
    export LD_LIBRARY_PATH=${pkgs.lib.makeLibraryPath dependencies}
  '';
}
