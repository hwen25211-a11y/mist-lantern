"""Compute Blender bone-heat weights on the unposed reconstruction mesh."""
from pathlib import Path
import bpy,json,hashlib
import numpy as np
from scipy.spatial import cKDTree
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Characters/Keeper-structured.blend'))
body=bpy.data.objects['Keeper runtime body']
spec=[('Body',None,(0,-.04,1.07),(0,-.13,1.80)),('Head','Body',(0,-.13,1.80),(0,-.13,2.39))]
for side,sign in [('R',-1),('L',1)]:
    spec.extend([('Arm'+side,'Body',(sign*.43,-.07,1.62),(sign*.66,-.12,1.28)),('Forearm'+side,'Arm'+side,(sign*.66,-.12,1.28),(sign*.79,-.20,1.00)),('Hand'+side,'Forearm'+side,(sign*.79,-.20,1.00),(sign*.76,-.36,.76)),('Leg'+side,'Body',(sign*.23,-.06,.90),(sign*.33,-.08,.59)),('Shin'+side,'Leg'+side,(sign*.33,-.08,.59),(sign*.36,-.04,.25)),('Foot'+side,'Shin'+side,(sign*.36,-.04,.25),(sign*.36,-.26,.10))])
data=bpy.data.armatures.new('Heat solve skeleton');arm=bpy.data.objects.new('Heat solve skeleton',data);bpy.context.collection.objects.link(arm)
bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);bpy.context.view_layer.objects.active=arm;bpy.ops.object.mode_set(mode='EDIT')
for name,parent,head,tail in spec:
    bone=data.edit_bones.new(name);bone.head=Vector(head);bone.tail=Vector(tail)
    if parent:bone.parent=data.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT');body.select_set(True)
bpy.ops.object.parent_set(type='ARMATURE_AUTO')
weights=[];missing=0
for v in body.data.vertices:
    w={body.vertex_groups[g.group].name:g.weight for g in v.groups if g.weight>1e-6}
    if not w:missing+=1
    weights.append(w)
print('BONE_HEAT_COVERAGE',len(weights)-missing,len(weights),'missing',missing,flush=True)
if missing>len(weights)*.01:raise RuntimeError('Bone-heat solve left more than one percent of vertices unbound')
positions=np.array([tuple(v.co)for v in body.data.vertices],dtype=np.float32)
if missing:
    bound=np.array([i for i,w in enumerate(weights)if w]);unbound=np.array([i for i,w in enumerate(weights)if not w])
    _,nearest=cKDTree(positions[bound]).query(positions[unbound])
    for i,j in zip(unbound,nearest):weights[i]=dict(weights[int(bound[j])])
output=ROOT/'Build/CharacterSource';output.mkdir(parents=True,exist_ok=True)
(output/'Keeper-heat-weights.json').write_text(json.dumps({'mesh_position_sha256':hashlib.sha256(positions.tobytes()).hexdigest(),'weights':weights,'nearest_bound_repairs':missing},separators=(',',':')))
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Build/CharacterSource/Keeper-heat-study.blend'))
print('BONE_HEAT_WEIGHTS_SAVED',flush=True)
