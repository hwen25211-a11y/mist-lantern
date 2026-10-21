#!/bin/zsh
set -euo pipefail
task_root="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$task_root"
character_python="${KEEPER_PYTHON:-/Users/Admin/.codex/deps/blender-runtime/venv/bin/python}"
character_unity="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/2022.3.62f3c1/Unity.app/Contents/MacOS/Unity}"
mkdir -p docs/qa/character-rebuild Build/CharacterSource
"$character_python" Tools/character/review_reference_mesh.py ArtSource/Characters/Generated/HunyuanMLX/keeper-shape-candidate.glb --out Build/CharacterReview/HunyuanMLX > docs/qa/character-rebuild/keeper-geometry.log 2>&1
for stage in paint_reference_shape bake_keeper structure_keeper heat_bind_keeper rig_keeper; do
    "$character_python" "Tools/character/$stage.py" > "docs/qa/character-rebuild/$stage.log" 2>&1
done
"$character_unity" -batchmode -projectPath "$task_root" -executeMethod Mist.Editor.ReleasePreparation.Run -quit -silent-test -logFile "$task_root/docs/qa/character-rebuild/preparation.log"
