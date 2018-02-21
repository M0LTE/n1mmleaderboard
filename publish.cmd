@echo off
dotnet publish n1mmlistener -r win-x64 -o ..\pub\win-x64
dotnet publish n1mmlistener -r win-x86 -o ..\pub\win-x86
