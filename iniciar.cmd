@echo off
cd /d "%~dp0"
dotnet run --project "ReproductorMusica\ReproductorMusica.csproj"
if errorlevel 1 pause
