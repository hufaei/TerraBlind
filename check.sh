#!/bin/sh
# Type-check the mod WITHOUT packing it, so it works while tModLoader is running.
#
# A full `dotnet build` runs the C# compile fine but then fails at the pack step with TML003
# ("close tModLoader to build mods directly"). -t:Compile stops before packing: every compile
# error still surfaces, in under a second, with no game restart and nobody typing /build.
#
# This does NOT replace the in-game build — it only proves the code compiles. Loading the new
# code into the running game is still `/build TerraBlind` in chat.
cd "$(dirname "$0")" || exit 1
if ! command -v dotnet >/dev/null 2>&1; then
	echo "error: dotnet is required for the C# compile check" >&2
	exit 127
fi

compile_log="${TMPDIR:-/tmp}/terrablind-compile-$$.log"
trap 'rm -f "$compile_log"' EXIT HUP INT TERM
if ! dotnet build -t:Compile -v q --nologo -nowarn:ChangeMagicNumberToID >"$compile_log" 2>&1; then
	grep -Ev "^$" "$compile_log" | tail -20
	exit 1
fi
grep -Ev "^$" "$compile_log" | tail -20
# 编译过了不代表原语交了失败现场 —— 那个只能靠这个查
./stuck_contract.sh || exit 1
