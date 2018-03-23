@echo off
set dashdb=C:\Users\tf\Desktop\database.db
pushd C:\Users\tf\Desktop\n1mmleaderboard
pushd bin\debug\netcoreapp2.0
start dotnet n1mm-udp-listener.dll C:\Users\tf\Desktop\database.db
popd
pushd leaderboard-web
dotnet publish
pushd bin\Debug\netcoreapp2.0\netcoreapp2.0\publish
start dotnet leaderboard-web.dll
start http://localhost:5000/
popd
popd