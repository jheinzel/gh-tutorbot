#!/usr/bin/env bash
dotnet publish . --configuration Release --self-contained --runtime win-x64 -p:PublishSingleFile=true --output ./dist



