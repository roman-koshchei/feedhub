#!/bin/bash
set -e

info() { 
  echo -e "\e[32m$*\e[0m"
}

# Homelab user and local IP
SERVER="rk@192.168.31.10"
OUTPUT="./bin"
SERVER_OUTPUT="/var/lib/feedhub"

info "Building the project"
dotnet publish "./src/Web/Web.csproj" -c Release -r linux-x64 -o $OUTPUT

info "Copying project files to the server"
scp -r ./bin $SERVER:/tmp
scp ./config/feedhub.nix $SERVER:/tmp
scp ./src/Web/.env $SERVER:/tmp

info "Rebuilding server"
ssh -t $SERVER "
  sudo rm -rf ${SERVER_OUTPUT}/bin
  sudo mv /tmp/bin ${SERVER_OUTPUT}
  sudo mv /tmp/.env ${SERVER_OUTPUT}/bin/.env
  sudo mv /tmp/feedhub.nix /etc/nixos/services/feedhub.nix
  sudo nixos-rebuild switch
  sudo systemctl restart feedhub.service
"

info "Press any key to exit..."
read -n 1 -s  
info "Exiting..."