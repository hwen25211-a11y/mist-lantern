#!/usr/bin/env python3
"""Verify the current Keeper release against build identities and fresh play evidence."""
from pathlib import Path
import hashlib,json,plistlib,re,urllib.request
root=Path(__file__).resolve().parents[1]
qa=root/'docs/qa';current=qa/'keeper-release';version='1.1.0'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
assert 'RULES_OK assertions=44' in (qa/'rules.txt').read_text()
assert 'COMBAT_OK assertions=34' in (qa/'combat-v2.txt').read_text()
play=(qa/'play-results.txt').read_text();assert 'PLAY_OK errors=0 overflow=0' in play
for weapon in range(3):assert 'runtime complete all rooms with weapon '+str(weapon)+' room=11' in play
assert 'edge stretch below 6' in (qa/'character-rebuild/mesh-checks.txt').read_text()
assert 'MODEL_CHECKS_OK' in (qa/'sculpt-review/model-checks.txt').read_text()
native=json.loads((current/'native-victory.json').read_text())
assert native['muted'] and native['run']['phase']==5 and native['run']['settled'] and native['run']['room']==11
assert json.loads((current/'native-resume-check.json').read_text())['run_identical_after_process_restart']
menu=json.loads((current/'native-menu-check.json').read_text());assert menu['muted'] and menu['new_max_hp']==menu['previous_max_hp']+12 and menu['embers_after']==menu['embers_before']-menu['cost']
webflow=json.loads((current/'web-flow.json').read_text());assert any('phase=Won' in entry['message']for entry in webflow)
assert json.loads((current/'web-errors.json').read_text())==[]
info=plistlib.loads((root/'Build/macOS/雾灯遗城.app/Contents/Info.plist').read_bytes());assert info['CFBundleShortVersionString']==version
html=(root/'Build/WebGL/index.html').read_text();assert 'v'+version in html and 'productVersion:"'+version+'"' in html
identities={}
for kind in ['mac','web']:
 log=(qa/(kind+'.log')).read_text();assert 'MIST_BUILD_OK' in log and not re.search(r'Shader error|error CS\d+|Build failed:',log)
 manifest=json.loads((current/(kind+'-build.json')).read_text());assert manifest['version']==version
 for name,digest in manifest['runtime_sources'].items():assert sha(root/name)==digest,('source changed after build',kind,name)
 for name,digest in manifest['artifacts'].items():assert sha(root/name)==digest,('build changed',kind,name)
 identities[kind]=sha(current/(kind+'-build.json'))
http=[];web=root/'Build/WebGL'
for file in [web/'index.html']+sorted((web/'Build').glob('*')):
 if not file.is_file():continue
 relative=file.relative_to(web).as_posix()
 with urllib.request.urlopen('http://127.0.0.1:8921/'+relative,timeout=30)as response:data=response.read();mime=response.headers.get_content_type()
 assert data==file.read_bytes(),relative
 if file.suffix=='.wasm':assert mime=='application/wasm'
 http.append({'file':relative,'bytes':len(data),'mime':mime,'sha256':hashlib.sha256(data).hexdigest()})
result={'version':version,'build_manifest_sha256':identities,'http':http,'native_win_seed':native['run']['seed'],'checks_passed':True}
(current/'release-verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2))
print(json.dumps({'version':version,'http_files':len(http),'checks_passed':True},ensure_ascii=False))
