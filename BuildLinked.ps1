# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/spelunky.script.archipelago/*" -Force -Recurse
dotnet publish "./spelunky.script.archipelago.csproj" -c Release -o "$env:RELOADEDIIMODS/spelunky.script.archipelago" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location
