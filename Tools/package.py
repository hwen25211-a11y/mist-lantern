#!/usr/bin/env python3
from pathlib import Path
import hashlib
import json
import zipfile
import stat

root = Path(__file__).resolve().parents[1]
release = root / "release"
release.mkdir(exist_ok=True)

def pack(name, files):
    path = release / name
    manifest = {}
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as out:
        for source, target in files:
            if not source.is_file() or source.name == ".DS_Store":
                continue
            out.write(source, target)
            manifest[target] = hashlib.sha256(source.read_bytes()).hexdigest()
    with zipfile.ZipFile(path) as archive:
        assert archive.testzip() is None
        assert all(hashlib.sha256(archive.read(n)).hexdigest() == digest for n,digest in manifest.items())
        if name.endswith("macOS.zip"):
            executable = next(x for x in archive.infolist() if "/Contents/MacOS/" in x.filename)
            assert (executable.external_attr >> 16) & stat.S_IXUSR
    return {"file": name, "bytes": path.stat().st_size, "entries": len(manifest),
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}

source = []
for folder in ["Assets", "Packages", "ProjectSettings", "Tools", "docs", "Licenses"]:
    for p in (root / folder).rglob("*"):
        if p.is_file() and p.suffix not in [".log", ".pyc"] and "__pycache__" not in p.parts:
            source.append((p, "雾灯遗城-Unity源码/" + str(p.relative_to(root))))
for name in ["README.md", ".gitignore"]:
    source.append((root/name, "雾灯遗城-Unity源码/"+name))
checks = [pack("雾灯遗城-v0.1.0-Unity源码.zip", source)]
app = root / "Build/macOS/雾灯遗城.app"
if app.exists():
    checks.append(pack("雾灯遗城-v0.1.0-macOS.zip", [(p,"雾灯遗城.app/"+str(p.relative_to(app))) for p in app.rglob("*") if p.is_file()]))
web = root / "Build/WebGL"
if (web/"index.html").exists():
    files = [(p,"WebGL/"+str(p.relative_to(web))) for p in web.rglob("*") if p.is_file()]
    files += [(root/"Tools/preview.py","Tools/preview.py"),
              (root/"Tools/启动浏览器版.command","启动浏览器版.command"),
              (root/"Tools/启动浏览器版.bat","启动浏览器版.bat"),
              (root/"docs/PLAY.txt","开始前请读我.txt")]
    checks.append(pack("雾灯遗城-v0.1.0-浏览器版.zip", files))
(release/"checksums.json").write_text(json.dumps(checks,ensure_ascii=False,indent=2))
print(json.dumps(checks,ensure_ascii=False,indent=2))
