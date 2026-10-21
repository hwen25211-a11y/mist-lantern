#!/bin/zsh
set -eu
cd "${0:A:h:h}"
ulimit -n 8192
UNITY_BIN="${UNITY_BIN:-/Applications/Unity/Hub/Editor/2022.3.62f3c1/Unity.app/Contents/MacOS/Unity}"
case "${1:-mac}" in
 prepare) method=Mist.Editor.BuildGame.Prepare; flags=(-quit -nographics) ;;
 models) method=Mist.Editor.BuildGame.GenerateModels; flags=(-quit -nographics) ;;
 gallery) method=Mist.Editor.BuildGame.Gallery; flags=(-quit) ;;
 mac) method=Mist.Editor.BuildGame.Mac; flags=(-quit -nographics) ;;
 web) method=Mist.Editor.BuildGame.Web; flags=(-quit -nographics) ;;
 checks) method=Mist.Editor.RuleChecks.Run; flags=(-quit -nographics) ;;
 smoke) method=Mist.Editor.PlayChecks.Run; flags=() ;;
 *) echo 'Usage: Tools/build.sh [prepare|mac|web|checks|smoke]'; exit 2 ;;
esac
"$UNITY_BIN" -batchmode "${flags[@]}" -projectPath "$PWD" -executeMethod "$method" -silent-test -logFile "$PWD/docs/qa/${1:-mac}.log"
case "${1:-mac}" in
 mac|web) python3 Tools/build_manifest.py "${1:-mac}" ;;
esac
