"""Make a review candidate from the approved front image on Apple Silicon.

Uses the pinned, MIT-licensed Hunyuan3D-Swift MLX port. Model weights retain
Tencent's separate community license. This creates shape only, not a finished
textured/rigged game asset. No network requests or credentials are used here.
"""
from pathlib import Path
import argparse
import hashlib
import importlib.metadata
import json
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
ap = argparse.ArgumentParser()
ap.add_argument('--source', type=Path, required=True)
ap.add_argument('--weights', type=Path, required=True)
ap.add_argument('--resolution', type=int, default=384)
ap.add_argument('--seed', type=int, default=43192)
args = ap.parse_args()
sys.path.insert(0, str(args.source / 'python/shape'))
import mlx.core as mx
from hy3dmlx.pipeline import Hunyuan3DShapePipeline

image = ROOT / 'ArtSource/Characters/Concept/keeper-model-front-v1.png'
out = ROOT / 'ArtSource/Characters/Generated/HunyuanMLX'
out.mkdir(parents=True, exist_ok=True)
record = {
    'status': 'starting', 'shape_only': True,
    'port': 'https://github.com/ZimengXiong/Hunyuan3D-Swift',
    'port_revision': '292331f4d26ddb80b9dcea6bcb5629ff82f12b82',
    'model': 'zimengxiong/hunyuan3d-mlx-shape-large',
    'model_revision': '5ad04593521ff80875ac007ef8185209368f18bc',
    'input': str(image.relative_to(ROOT)),
    'input_sha256': hashlib.sha256(image.read_bytes()).hexdigest(),
    'seed': args.seed, 'steps': 8, 'quantize': 8,
    'resolution': args.resolution, 'query_chunk': 4096,
    'mlx_version': importlib.metadata.version('mlx'),
}
started = time.monotonic()
def save(stage):
    record['status'] = stage
    record['elapsed_seconds'] = round(time.monotonic() - started, 2)
    (out / 'generation.json').write_text(json.dumps(record, indent=2))
    print('LOCAL_SHAPE_STAGE', stage, record['elapsed_seconds'], flush=True)

try:
    mx.set_memory_limit(7 * 1024**3)
    mx.set_cache_limit(256 * 1024**2)
    save('loading quantized models')
    pipe = Hunyuan3DShapePipeline.from_pretrained(str(args.weights),
                                                dtype=mx.float16, quantize=8)
    save('generating shape')
    mesh = pipe.generate(str(image), num_inference_steps=8, guidance_scale=5.0,
                         octree_resolution=args.resolution, num_chunks=4096,
                         seed=args.seed, octree_decode=True)
    target = out / 'keeper-shape-candidate.glb'
    mesh.export(target)
    record.update(vertices=len(mesh.vertices), triangles=len(mesh.faces),
                  bounds=mesh.bounds.tolist(), watertight=bool(mesh.is_watertight),
                  output=str(target.relative_to(ROOT)),
                  output_sha256=hashlib.sha256(target.read_bytes()).hexdigest(),
                  peak_mlx_gb=round(mx.get_peak_memory()/1024**3, 3))
    save('candidate generated; visual, material, rig and runtime review required')
except BaseException as error:
    record['error'] = repr(error)
    save('failed')
    raise
