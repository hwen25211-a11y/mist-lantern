# Keeper character source

The approved target is `Concept/keeper-cartoon-v1.png`. Concept and paint-guide PNGs are AI-assisted references; actual model renders are under `docs/screenshots/character-rebuild/`.

## Current asset

- `Keeper.blend`: A-pose textured body, 180,000 triangles, editable UVs and 4096px albedo.
- `Keeper-structured.blend`: separate solid copper shoulder shell and flexible dark sleeve.
- `Keeper-rigged.blend`: holding pose with 17 named animation controls and four weights per vertex.
- `KeeperAlbedo.png`: atlas baked from four geometry-aligned projections with occluded-surface repairs.
- `Keeper-bake.json` and `Keeper-rig.json`: generated geometry counts; these stage records are not release acceptance.
- `Assets/Resources/Characters/Warden.prefab`: imported runtime mesh. The historic Warden name is retained for code compatibility.
- Held cleaver, lantern, rear roll and pouch are true separate geometry authored in `Assets/Scripts/SculptKeeperWeapon.cs`.

The shape was reconstructed locally on an Apple M1 Pro using Hunyuan3D-Swift / MLX, then reduced, painted, repaired, bound and integrated. It is **AI-assisted reconstruction, not a student-sculpted-from-scratch character**. Face, hair, clothing and boots have actual volume; texture projection supplies the graphic painted detail.

## Rebuild

Use `Tools/character/rebuild.sh`. Set `KEEPER_PYTHON` to a Python 3.11 environment with `bpy==4.5.10`, `numpy==1.26.4`, `scipy==1.16.3` and Pillow. Set `UNITY_EDITOR` if Unity 2022.3.62f3c1 is elsewhere. The script starts from the committed GLB, recreates intermediate review files, paint, atlas, armor, bone heat, rig, Unity assets and checks; it stops on the first failure. No cloud service or model download is required to rebuild from that GLB.

Shape re-generation is optional and separate: `generate_local_shape.py` uses the external MLX environment and verified weights. Exact revisions, checksum, inference settings and Tencent/port licenses are preserved in `Generated/HunyuanMLX/`. Model weights are not redistributed in this repository.

Bone heat is corrected for opposite-arm leakage and garment ownership, smoothed over length-weighted mesh adjacency, and checked for excessive edge stretching. Unbound isolated vertices inherit the nearest valid binding. A source-position hash prevents reusing weights for another mesh. Unity checks the imported skin, all weapon variants, windup/recovery and bent-leg poses.

## Rejected studies

The earlier MakeHuman-based realistic/manual studies were rejected and are excluded from current source delivery and game assets. They remain locally under ignored `Build/RejectedCharacterStudies/ManualSource/` with their source attribution. The current Keeper uses no MakeHuman geometry or hair.

Cloud Pixal3D produced a preview but no export: its 240-second GPU extraction request exceeded the service's per-request limit. `Generated/Pixal3D/browser-generation.json` records that outcome. This project contains no downloaded Pixal3D mesh.
