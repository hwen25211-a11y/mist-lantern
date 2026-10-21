"""Bind the reconstructed keeper to the game's named animation controls.

The exported body is a deforming textured mesh. Held tools and rear equipment
remain separate geometry. The A-pose source is preserved in Keeper.blend.
"""
from pathlib import Path
import bpy,json,math,hashlib
import numpy as np
from PIL import Image
from scipy.sparse import coo_matrix,diags
from mathutils import Vector,Matrix

ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtSource/Characters'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Keeper-structured.blend'))
body=bpy.data.objects['Keeper runtime body'];me=body.data
definitions=[('Rig',None,(0,0,0)),('Body','Rig',(0,-.04,1.07)),('Head','Body',(0,-.13,1.80)),('Cape','Body',(0,.29,1.55)),('CapeTip','Cape',(0,.45,1.02))]
for side,sign in [('R',-1),('L',1)]:
    definitions.extend([('Arm'+side,'Body',(sign*.43,-.07,1.62)),('Forearm'+side,'Arm'+side,(sign*.66,-.12,1.28)),('Hand'+side,'Forearm'+side,(sign*.79,-.20,1.00)),('Leg'+side,'Rig',(sign*.23,-.06,.90)),('Shin'+side,'Leg'+side,(sign*.33,-.08,.59)),('Foot'+side,'Shin'+side,(sign*.36,-.04,.25))])
definitions=[(n,p,Vector(v))for n,p,v in definitions];names=[x[0]for x in definitions];index={n:i for i,n in enumerate(names)};position={n:v for n,p,v in definitions}
uv=me.uv_layers.active;pixels=np.asarray(Image.open(OUT/'KeeperAlbedo.png').convert('RGB'))/255.;vertex_uv=np.zeros((len(me.vertices),2))
for loop in me.loops:vertex_uv[loop.vertex_index]=uv.data[loop.index].uv
px=np.clip((vertex_uv[:,0]*pixels.shape[1]).astype(int),0,pixels.shape[1]-1);py=np.clip(((1-vertex_uv[:,1])*pixels.shape[0]).astype(int),0,pixels.shape[0]-1);pigment=pixels[py,px]
def smooth(a,b,v):
    t=max(0,min(1,(v-a)/(b-a)));return t*t*(3-2*t)
def normalize(w):
    w={n:v for n,v in w.items() if v>1e-6};items=sorted(w.items(),key=lambda v:-v[1])[:4];total=sum(v for _,v in items)
    return {n:v/total for n,v in items} if total else {'Body':1}
def blend(a,b,t):
    w={n:v*(1-t)for n,v in a.items()}
    for n,v in b.items():w[n]=w.get(n,0)+v*t
    return normalize(w)
weights=[]
for vertex,rgb in zip(me.vertices,pigment):
    p=vertex.co;side='R' if p.x<0 else 'L';x=abs(p.x)
    base=blend({'Rig':1},{'Body':1},smooth(.97,1.12,p.z))
    warm=rgb[0]>rgb[1]*1.07 and rgb[0]>rgb[2]*1.10
    teal=rgb[1]>rgb[0]*1.15 and rgb[2]>rgb[0]*.95
    copper=rgb[0]>rgb[1]*1.18 and rgb[1]>rgb[2]*1.20 and rgb[1]>.20
    arm_boundary=float(np.interp(p.z,[.55,.95,1.30,1.65,1.80],[.61,.61,.45,.30,.30]))
    if p.z>2.02 or (p.z>1.76 and warm and p.y<.18 and x<.43):
        w=blend(base,{'Head':1},smooth(1.76,1.88,p.z))
    elif .56<p.z<1.90 and (p.y>.24 or teal):
        tail=blend({'CapeTip':1},{'Cape':1},smooth(.83,1.35,p.z))
        w=blend(tail,{'Body':1},max(smooth(1.40,1.66,p.z),1-smooth(.12,.25,p.y)))
    elif side=='R' and copper and 1.38<p.z<1.79 and x>.29 and p.y<.23:
        w={'ArmR':1}
    elif .55<p.z<1.80 and x>arm_boundary and p.y<.23:
        upper={'Arm'+side:1};fore={'Forearm'+side:1};hand={'Hand'+side:1}
        limb=blend(hand,fore,smooth(.93,1.08,p.z));limb=blend(limb,upper,smooth(1.20,1.40,p.z))
        w=blend(base,limb,smooth(arm_boundary,arm_boundary+.035,x))
    elif p.z<.87:
        leg=blend({'Foot'+side:1},{'Shin'+side:1},smooth(.21,.34,p.z))
        leg=blend(leg,{'Leg'+side:1},smooth(.51,.68,p.z))
        if copper and .50<p.z<.72 and p.y<-.18:leg={'Shin'+side:1}
        w=blend(leg,base,smooth(.77,.87,p.z))
    else:w=base
    weights.append(normalize(w))
heat_path=ROOT/'Build/CharacterSource/Keeper-heat-weights.json'
if heat_path.exists():
    heat=json.loads(heat_path.read_text())
    if isinstance(heat,dict):
        signature=hashlib.sha256(np.array([tuple(v.co)for v in me.vertices],dtype=np.float32).tobytes()).hexdigest()
        if heat['mesh_position_sha256']!=signature:raise ValueError('Bone heat mesh identity changed; rebuild heat weights')
        heat=heat['weights']
    if len(heat)!=len(me.vertices):raise ValueError('Heat weights must be rebuilt after changing the runtime mesh')
    for i,(v,heat_weight) in enumerate(zip(me.vertices,heat)):
        p=v.co
        w=normalize(heat_weight) if heat_weight else weights[i]
        # Bone heat can leak through the fused cloak into the opposite arm.
        # Keep those weights on the torso; image pigment must not decide joints.
        for side,sign in [('R',-1),('L',1)]:
            for bone in ['Arm'+side,'Forearm'+side,'Hand'+side]:
                amount=w.get(bone,0)*(1-smooth(.06,.30,sign*p.x))
                w[bone]=w.get(bone,0)-amount;w['Body']=w.get('Body',0)+amount
            for bone in ['Leg'+side,'Shin'+side,'Foot'+side]:
                amount=w.get(bone,0)*smooth(.86,1.08,p.z)
                w[bone]=w.get(bone,0)-amount;w['Body']=w.get('Body',0)+amount
        amount=w.get('Head',0)*(1-smooth(1.48,1.80,p.z))
        w['Head']=w.get('Head',0)-amount;w['Body']=w.get('Body',0)+amount
        collar=smooth(1.52,1.82,p.z)*(1-smooth(.27,.58,abs(p.x)))
        w=blend(w,{'Body':1},collar)
        if .25<p.z<1.69 and p.y>.20:
            tail=blend({'CapeTip':1},{'Cape':1},smooth(.83,1.35,p.z))
            cape=blend(tail,{'Body':1},smooth(1.40,1.66,p.z))
            w=blend(w,cape,smooth(.20,.28,p.y))
        if 'Body' in w:
            amount=w['Body']*(1-smooth(.92,1.12,p.z));w['Body']-=amount;w['Rig']=w.get('Rig',0)+amount
        weights[i]=normalize(w)
    print('USING_BLENDER_BONE_HEAT_WEIGHTS',flush=True)
# Smooth along actual surface adjacency, so a stray texture pixel cannot attach
# an isolated cape/coat vertex to a distant hand and create a stretched spike.
edge_indices=np.empty(len(me.edges)*2,dtype=np.int32);me.edges.foreach_get('vertices',edge_indices);edge_indices=edge_indices.reshape(-1,2)
rows=np.concatenate([edge_indices[:,0],edge_indices[:,1]]);cols=np.concatenate([edge_indices[:,1],edge_indices[:,0]])
coords=np.array([tuple(v.co)for v in me.vertices]);edge_length=np.linalg.norm(coords[edge_indices[:,0]]-coords[edge_indices[:,1]],axis=1)
conductance=1/np.maximum(edge_length,.0005)
adj=coo_matrix((np.concatenate([conductance,conductance]),(rows,cols)),shape=(len(me.vertices),len(me.vertices))).tocsr();adj=diags(1/np.maximum(np.asarray(adj.sum(1)).ravel(),1))@adj
dense=np.zeros((len(me.vertices),len(names)))
for i,w in enumerate(weights):
    for n,a in w.items():dense[i,index[n]]=a
for _ in range(120):dense=.55*dense+.45*(adj@dense)
weights=[normalize({names[i]:float(row[i])for i in np.flatnonzero(row>1e-5)})for row in dense]
original=np.array([tuple(v.co)for v in me.vertices])
rotations={n:Matrix.Identity(4)for n in names}
rotations['ArmL']=Matrix.Rotation(math.radians(-20),4,'Y')
rotations['ForearmL']=Matrix.Rotation(math.radians(-110),4,'X')
transforms={};posed_positions={}
for n,parent,p in definitions:
    inherited=transforms[parent] if parent else Matrix.Identity(4)
    transforms[n]=inherited@Matrix.Translation(p)@rotations[n]@Matrix.Translation(-p)
    posed_positions[n]=inherited@p
for n in names:body.vertex_groups.new(name=n)
for vertex,w in zip(me.vertices,weights):
    vertex.co=sum((transforms[n]@vertex.co*amount for n,amount in w.items()),Vector())
    for n,amount in w.items():body.vertex_groups[n].add([vertex.index],amount,'REPLACE')
posed=np.array([tuple(v.co)for v in me.vertices]);before=np.linalg.norm(original[edge_indices[:,0]]-original[edge_indices[:,1]],axis=1);after=np.linalg.norm(posed[edge_indices[:,0]]-posed[edge_indices[:,1]],axis=1)
ratio=after/np.maximum(before,.0005);worst=np.argsort(ratio)[-8:]
print('KEEPER_BASE_POSE_EDGE_RATIO',float(ratio.max()),'p999',float(np.quantile(ratio,.999)),flush=True)
for edge in worst[-3:]:print('EXTREME_EDGE',round(float(ratio[edge]),2),original[edge_indices[edge]].round(3).tolist(),[weights[i]for i in edge_indices[edge]],flush=True)
if ratio.max()>6 or np.quantile(ratio,.999)>3:raise RuntimeError('Neutral holding pose contains excessive mesh stretching')
bpy.context.view_layer.objects.active=body
if me.has_custom_normals:bpy.ops.mesh.customdata_custom_splitnormals_clear()
for face in me.polygons:face.use_smooth=True
me.update();me.calc_loop_triangles()
armdata=bpy.data.armatures.new('Keeper animation controls');arm=bpy.data.objects.new('Keeper rig',armdata);bpy.context.collection.objects.link(arm);bpy.context.view_layer.objects.active=arm;arm.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
for n,parent,_ in definitions:
    bone=armdata.edit_bones.new(n);bone.head=posed_positions[n];bone.tail=posed_positions[n]+Vector((0,0,.08))
    if parent:bone.parent=armdata.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT');arm.show_in_front=True
modifier=body.modifiers.new('Keeper deformation','ARMATURE');modifier.object=arm;body.parent=arm
def xyz(p):return [round(float(p.x),7),round(float(p.z),7),round(float(p.y),7)]
parts=[]
for ob in [body]+[o for o in bpy.context.scene.objects if o.type=='MESH' and o.get('rigid_bone')]:
    rigid=ob.get('rigid_bone');mesh=ob.data
    if rigid:
        group=ob.vertex_groups.new(name=rigid);group.add(list(range(len(mesh.vertices))),1,'REPLACE')
        for v in mesh.vertices:v.co=transforms[rigid]@v.co
        mesh.update();modifier=ob.modifiers.new('Rigid shoulder attachment','ARMATURE');modifier.object=arm;ob.parent=arm
    mesh.calc_loop_triangles();texlayer=mesh.uv_layers.active
    for mi,material in enumerate(mesh.materials):
        painted=mi==0;col=[1,1,1,1] if painted else list(material.diffuse_color)
        part={'name':ob.name if painted else material.name,'painted':painted,'alphaCutout':False,'texture':'ArtSource/Characters/KeeperAlbedo.png'if painted else '','roughness':.9,'metallic':0,'color':col,'vertices':[],'normals':[],'colors':[],'uv':[],'indices':[],'boneIndices':[],'boneWeights':[]};lookup={}
        for tri in mesh.loop_triangles:
            if tri.material_index!=mi:continue
            for loop_index in [tri.loops[0],tri.loops[2],tri.loops[1]]:
                vi=mesh.loops[loop_index].vertex_index;p=xyz(mesh.vertices[vi].co);normal_vector=mesh.corner_normals[loop_index].vector
                # At the solidified shell's narrow cut rim, do not average the
                # inner and outer surface normals across the hard metal edge.
                if rigid and normal_vector.dot(tri.normal)<.05:normal_vector=tri.normal
                normal=xyz(normal_vector);tex=[round(float(x),7)for x in texlayer.data[loop_index].uv]
                assigned={rigid:1}if rigid else weights[vi];w=list(assigned.items())+[(names[0],0)]*(4-len(assigned));bi=[index[n]for n,a in w];bw=[round(a,7)for n,a in w]
                key=tuple(p+normal+tex+bi+bw)
                if key not in lookup:
                    lookup[key]=len(part['vertices'])//3;part['vertices'].extend(p);part['normals'].extend(normal);part['colors'].extend(col);part['uv'].extend(tex);part['boneIndices'].extend(bi);part['boneWeights'].extend(bw)
                part['indices'].append(lookup[key])
        if part['indices']:parts.append(part)
grips={}
for side,sign in [('R',-1),('L',1)]:grips[side]=xyz(transforms['Hand'+side]@Vector((sign*.80,-.29,.91)))
document={'version':1,'bones':[{'name':n,'parent':index[parent]if parent else -1,'position':xyz(posed_positions[n])}for n,parent,p in definitions],'parts':parts,'gripRight':grips['R'],'gripLeft':grips['L']}
(ROOT/'Build/CharacterSource').mkdir(parents=True,exist_ok=True)
(ROOT/'Build/CharacterSource/Warden.mesh.json').write_text(json.dumps(document,separators=(',',':')))
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Keeper-rigged.blend'))
vertex_count=sum(len(p['vertices'])//3 for p in parts);triangle_count=sum(len(p['indices'])//3 for p in parts)
(OUT/'Keeper-rig.json').write_text(json.dumps({'bones':len(definitions),'vertices':vertex_count,'triangles':triangle_count,'parts':len(parts),'status':'bound candidate; full visual/deformation/runtime checks required'},indent=2))
print('KEEPER_SKIN_EXPORT_OK',vertex_count,triangle_count,len(definitions),flush=True)
