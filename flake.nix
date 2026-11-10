{
  description = "Development environment for Space Station 14";

  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-26.05";

  outputs =
    {
      self,
      nixpkgs,
    }:
    let
      supportedSystems = [
        "aarch64-linux"
        "aarch64-darwin"
        "x86_64-darwin"
        "x86_64-linux"
      ];
      forEachSupportedSystem =
        f:
        nixpkgs.lib.genAttrs supportedSystems (
          system:
          f ({
            pkgs = nixpkgs.legacyPackages.${system};
          })
        );
    in
    {
      devShells = forEachSupportedSystem (
        { pkgs }:
        {
          default = import ./shell.nix { inherit pkgs; };
        }
      );
    };
}
