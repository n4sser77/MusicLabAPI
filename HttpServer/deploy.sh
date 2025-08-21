#!/bin/bash

# Build
dotnet publish ".\MusiclabAPI.csproj" -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true


# Copy to server
scp -i ~/MyVpsSshKeys -r ./bin/Release/net9.0/publish/* deploy@82.165.141.93:/var/www/musiclabapi

# Restart the service
ssh -i ~/MyVpsSshKeys deploy@82.165.141.93 "sudo systemctl restart musiclabapi"

echo "Deployment done!"

