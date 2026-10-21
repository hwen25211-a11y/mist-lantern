#!/usr/bin/env python3
"""Verify the actual release files, reports, source identity and local HTTP payloads."""
from pathlib import Path
import hashlib
import json
import plistlib
import re
import urllib.request

root = Path(__file__).resolve().parents[1]
version = "1.0.0"
qa = root / "docs/qa"
assert "RULES_OK assertions=44" in (qa / "rules.txt").read_text()
assert "COMBAT_OK assertions=34" in (qa / "combat-v2.txt").read_text()
assert "PLAY_OK errors=0 overflow=0" in (qa / "play-results.txt").read_text()
native = json.loads((qa / "native-victory.json").read_text())
assert native["muted"] and native["run"]["phase"] == 5 and native["run"]["settled"]
assert json.loads((qa / "native-resume-check.json").read_text())["run_identical_after_process_restart"]
assert json.loads((qa / "native-menu-check.json").read_text())["run"]["maxHp"] == 132
assert json.loads((qa / "web-v1-errors.json").read_text()) == []
assert any("phase=Won" in entry["message"] for entry in json.loads((qa / "web-v1-flow.json").read_text()))
info = plistlib.loads((root / "Build/macOS/雾灯遗城.app/Contents/Info.plist").read_bytes())
assert info["CFBundleShortVersionString"] == version
html = (root / "Build/WebGL/index.html").read_text()
assert "v" + version in html and 'productVersion:"' + version + '"' in html
for kind in ["web", "mac"]:
    text = (qa / (kind + ".log")).read_text()
    assert "MIST_BUILD_OK" in text
    assert not re.search(r"Shader error|error CS\d+|Build failed:", text)

sources = {}
for folder, glob in [("Assets/Scripts", "*.cs"), ("Assets/Shaders", "*.shader")]:
    for file in (root / folder).glob(glob):
        sources[str(file.relative_to(root))] = hashlib.sha256(file.read_bytes()).hexdigest()
wasm = next((root / "Build/WebGL/Build").glob("*.wasm"))
data_file = next((root / "Build/WebGL/Build").glob("*.data"))
# Unity copies the HTML template with its original timestamp; compare compiled artifacts instead.
assert max((root / p).stat().st_mtime for p in sources if p.endswith(".cs")) <= wasm.stat().st_mtime
assert max((root / p).stat().st_mtime for p in sources if p.endswith(".shader")) <= data_file.stat().st_mtime

http = []
web = root / "Build/WebGL"
for file in [web / "index.html"] + sorted((web / "Build").glob("*")):
    if not file.is_file():
        continue
    relative = file.relative_to(web).as_posix()
    with urllib.request.urlopen("http://127.0.0.1:8920/" + relative, timeout=30) as response:
        data = response.read()
        mime = response.headers.get_content_type()
    assert data == file.read_bytes(), relative
    if file.suffix == ".wasm":
        assert mime == "application/wasm"
    http.append({"file": relative, "bytes": len(data), "mime": mime,
                 "sha256": hashlib.sha256(data).hexdigest()})

result = {"version": version, "source_sha256": sources, "http": http,
          "native_win_seed": native["run"]["seed"], "checks_passed": True}
(qa / "release-verification.json").write_text(json.dumps(result, ensure_ascii=False, indent=2))
print(json.dumps({"version": version, "http_files": len(http), "source_files": len(sources),
                  "checks_passed": True}, ensure_ascii=False))
