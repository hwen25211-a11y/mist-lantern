"""Generate a candidate mesh from the approved design via the public Pixal3D API.

Only the project's non-sensitive generated character image is sent. Anonymous
access is explicit: no local Hugging Face credential is read or transmitted.
No queue limits, authentication requirements, or service restrictions are bypassed.
"""
from pathlib import Path
import hashlib,json,shutil,uuid
from gradio_client import Client,handle_file

ROOT=Path(__file__).resolve().parents[2]
IMAGE=ROOT/'ArtSource/Characters/Concept/keeper-model-front-v1.png'
OUT=ROOT/'ArtSource/Characters/Generated/Pixal3D'
OUT.mkdir(parents=True,exist_ok=True)
record={'service':'https://tencentarc-pixal3d.hf.space','space':'TencentARC/Pixal3D','space_revision':'40a1519aadf70a82110a500997c19b626668a144','seed':43192,'resolution':1024,'input':str(IMAGE.relative_to(ROOT)),'input_sha256':hashlib.sha256(IMAGE.read_bytes()).hexdigest(),'session_id':str(uuid.uuid4()),'status':'starting'}
def save():
    (OUT/'generation.json').write_text(json.dumps(record,indent=2))
def stage(s):
    record['status']=s;save();print('PIXAL3D_STAGE',s,flush=True)
save()
try:
    client=Client(record['service'],token=False,download_files=OUT/'downloads',analytics_enabled=False,verbose=False,httpx_kwargs={'timeout':120})
    stage('preprocess')
    processed=client.predict(image=handle_file(str(IMAGE)),api_name='/preprocess')
    record['preprocessed']=processed;save()
    stage('generate shape and material')
    result=client.predict(image=handle_file(processed),seed=record['seed'],resolution=record['resolution'],session_id=record['session_id'],api_name='/generate_3d')
    record['generation_result']=result;save()
    stage('extract textured GLB')
    glb=client.predict(state_path=result['state_path'],decimation_target=160000,texture_size=2048,session_id=record['session_id'],api_name='/extract_glb_api')
    target=OUT/'keeper-reference-candidate.glb';shutil.copy2(glb,target);record['glb']=str(target.relative_to(ROOT));record['glb_sha256']=hashlib.sha256(target.read_bytes()).hexdigest();stage('candidate generated; visual and geometry review required')
except Exception as error:
    record['status']='failed';record['error']=str(error);save();print('PIXAL3D_FAILED',type(error).__name__,str(error),flush=True);raise
