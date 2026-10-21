#!/usr/bin/env python3
from pathlib import Path
import hashlib
import json
import zipfile
import stat
import subprocess
import sys

root = Path(__file__).resolve().parents[1]
subprocess.run([sys.executable, str(root / "Tools/verify_release.py")], cwd=root, check=True)
release = root / "release"
release.mkdir(exist_ok=True)
commit = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip() if (root/".git").exists() else "source-archive"
build_info = release / "BUILD-INFO.txt"
build_info.write_text("雾灯遗城 v1.1.0\nUnity 2022.3.62f3c1\n验证日期：2026-10-21\n源码提交："+commit+"\n\n包含 macOS 与 WebGL 发布构建。默认静音。\n完整测试记录：Unity 源码包 docs/QA.md。\n素材与 AI 辅助绘画说明：docs/ART-V1.md。\n压缩包完整性与文件 SHA256：checksums.json。\n", encoding="utf-8")

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
for folder in ["Assets", "Packages", "ProjectSettings", "Tools", "docs", "Licenses", "ArtSource"]:
    for p in (root / folder).rglob("*"):
        if p.is_file() and p.suffix not in [".log", ".pyc"] and "__pycache__" not in p.parts:
            source.append((p, "雾灯遗城-Unity源码/" + str(p.relative_to(root))))
for name in ["README.md", ".gitignore", ".gitattributes"]:
    source.append((root/name, "雾灯遗城-Unity源码/"+name))
source.append((build_info,"雾灯遗城-Unity源码/BUILD-INFO.txt"))
checks = [pack("雾灯遗城-v1.1.0-Unity源码.zip", source)]
notices = [(p,"Licenses/"+str(p.relative_to(root/"Licenses"))) for p in (root/"Licenses").rglob("*") if p.is_file()]
for name in ["MODEL-LICENSE.txt","MODEL-NOTICE.txt","PORT-LICENSE.txt"]:
    notices.append((root/"ArtSource/Characters/Generated/HunyuanMLX"/name,"Licenses/HunyuanMLX/"+name))
app = root / "Build/macOS/雾灯遗城.app"
if app.exists():
    files = [(p,"雾灯遗城.app/"+str(p.relative_to(app))) for p in app.rglob("*") if p.is_file()]
    files += [(build_info,"BUILD-INFO.txt"),(root/"docs/PLAY.txt","开始前请读我.txt")] + notices
    checks.append(pack("雾灯遗城-v1.1.0-macOS.zip", files))
web = root / "Build/WebGL"
if (web/"index.html").exists():
    files = [(p,"WebGL/"+str(p.relative_to(web))) for p in web.rglob("*") if p.is_file()]
    files += [(root/"Tools/preview.py","Tools/preview.py"),
              (root/"Tools/启动浏览器版.command","启动浏览器版.command"),
              (root/"Tools/启动浏览器版.bat","启动浏览器版.bat"),
              (root/"docs/PLAY.txt","开始前请读我.txt")]
    files.append((build_info,"BUILD-INFO.txt"))
    files += notices
    checks.append(pack("雾灯遗城-v1.1.0-浏览器版.zip", files))
(release/"checksums.json").write_text(json.dumps(checks,ensure_ascii=False,indent=2))
print(json.dumps(checks,ensure_ascii=False,indent=2))
