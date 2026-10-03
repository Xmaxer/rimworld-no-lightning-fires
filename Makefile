# No Lightning Fires: dev commands. Wraps the PowerShell scripts in tools/. Run `make help` for the list.

PS := powershell -NoProfile -ExecutionPolicy Bypass -File

SCENARIOS ?= all

.PHONY: help build package deploy play smoke clean unlink

help:
	@echo "Targets:"
	@echo "  make build    - dotnet build Source -c Release"
	@echo "  make package  - build, then assemble dist/NoLightningFires"
	@echo "  make deploy   - build + package + junction the mod and dev harness into RimWorld/Mods"
	@echo "  make play     - deploy, then launch an isolated dev profile; a NEW colony gets a flashstorm over a grass field"
	@echo "  make smoke    - full automated in-game test run (needs Steam running, RimWorld closed)"
	@echo "                  pick scenarios: make smoke SCENARIOS=strike,flashstorm"
	@echo "  make unlink   - remove the Mods-folder junctions this project created"
	@echo "  make clean    - remove build output, dist/ and the smoke-test profile"

build:
	dotnet build Source -c Release

package: build
	$(PS) tools/package.ps1

deploy:
	$(PS) tools/deploy-dev.ps1

play:
	$(PS) tools/play.ps1

smoke:
	$(PS) tools/smoke-test.ps1 -Scenarios $(SCENARIOS)

unlink:
	powershell -NoProfile -Command "foreach ($$n in 'NoLightningFires','NoLightningFiresDevHarness') { $$p = Join-Path 'C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods' $$n; if (Test-Path $$p) { cmd /c rmdir $$p; Write-Output \"Removed $$p\" } }"

clean:
	powershell -NoProfile -Command "Remove-Item -Recurse -Force -ErrorAction SilentlyContinue dist, 1.6/Assemblies, tools/DevHarness/1.6/Assemblies, tools/.smoke-profile, Source/*/bin, Source/*/obj"
