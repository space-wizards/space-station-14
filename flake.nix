{

  inputs.nixpkgs.url = "github:NixOS/nixpkgs";
  inputs.flake-utils.url = "github:numtide/flake-utils";

  outputs = { self, nixpkgs, flake-utils, ... }:
    flake-utils.lib.simpleFlake {
      inherit self nixpkgs;
      name = "space-station-14-devshell";
      shell = ./shell.nix;
    };

}
