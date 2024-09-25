#!/bin/bash
set -e

# Homelab user and local IP
SERVER="rk@192.168.31.10"
OUTPUT="./bin"
SERVER_OUTPUT="/var/lib/feedhub"

echo "Building the project"
dotnet publish "./src/Web/Web.csproj" -c Release -r linux-x64 -o $OUTPUT

echo "Copying project files to the server"
scp -r ./bin $SERVER:/tmp
scp ./config/feedhub.nix $SERVER:/tmp
scp ./src/Web/.env $SERVER:/tmp

ssh -t $SERVER "
  sudo rm -rf /var/lib/feedhub/bin
  sudo mv /tmp/bin /var/lib/feedhub
  sudo mv /tmp/.env /var/lib/feedhub/bin/.env
  sudo mv /tmp/feedhub.nix /etc/nixos/services/feedhub.nix
  sudo nixos-rebuild switch
  sudo systemctl restart feedhub-blue.service
"