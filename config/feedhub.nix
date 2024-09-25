{ config, pkgs, ... }:

let
  bluePort = 6100;
  greenPort = bluePort + 1;

  feedhub = port: {
    enable = true;
    description = "Feedhub systemd instance";

    wantedBy = [ "multi-user.target" ];

    environment = {
      ASPNETCORE_URLS = "http://localhost:${toString port}";
    };

    # env is currently in .env file right beside project files
    serviceConfig = {
      Type = "simple";
      Restart = "always";
      StateDirectory = "feedhub";
      WorkingDirectory = "/var/lib/feedhub/bin";
      ExecStart = "${pkgs.dotnet-aspnetcore_8}/bin/dotnet ./Web.dll";
      RestartSec = 0;
    };
  };
in {
  # 2 instances
  systemd.services.feedhub-blue = feedhub bluePort;
  # can't 
  # systemd.services.feedhub-green = feedhub greenPort;

  services.caddy.virtualHosts."feedhub.cookingweb.dev".extraConfig = ''
    reverse_proxy :${toString bluePort}
  '';
    
}