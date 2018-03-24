@echo off
set dashdb=C:\Users\tf\Desktop\database.db
pushd C:\Users\tf\Desktop\n1mmleaderboard
pushd bin\debug\netcoreapp2.0
start dotnet n1mm-udp-listener.dll C:\Users\tf\Desktop\database.db
popd
popd