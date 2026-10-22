#!/usr/bin/env python3
"""Bind runtime source assets and actual build bytes to one successful build."""
from pathlib import Path
import hashlib,json,sys,datetime

root=Path(__file__).resolve().parents[1]
kind=sys.argv[1]
assert kind in ('mac','web')
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
folders=['Assets/Scripts','Assets/Shaders','Assets/Characters','Assets/Models',
         'Assets/Resources/Characters','Assets/Resources/Models',
         'Assets/Resources/Stages','Assets/Resources/Art']
sources={str(p.relative_to(root)):sha(p)for folder in folders for p in sorted((root/folder).rglob('*'))if p.is_file()and p.name!='.DS_Store'}
build=root/('Build/macOS/雾灯遗城.app'if kind=='mac'else 'Build/WebGL')
artifacts={str(p.relative_to(root)):sha(p)for p in sorted(build.rglob('*'))if p.is_file()and p.name!='.DS_Store'}
assert artifacts
out=root/'docs/qa/combat-polish';out.mkdir(parents=True,exist_ok=True)
(out/(kind+'-build.json')).write_text(json.dumps({'version':'1.2.0','built_at':datetime.datetime.now(datetime.timezone.utc).isoformat(),'runtime_sources':sources,'artifacts':artifacts},ensure_ascii=False,indent=2))
print('BUILD_MANIFEST_OK',kind,'sources',len(sources),'artifacts',len(artifacts))
