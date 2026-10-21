"""Separate the shoulder shell from flexible clothing before animation."""
from pathlib import Path
import bpy,numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtSource/Characters'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Keeper.blend'))
body=next(o for o in bpy.context.scene.objects if o.type=='MESH');me=body.data;uv=me.uv_layers.active
pixels=np.asarray(Image.open(OUT/'KeeperAlbedo.png').convert('RGB'))/255.
selected=[]
for polygon in me.polygons:
    center=polygon.center
    if not(-.79<center.x<-.27 and 1.37<center.z<1.79 and center.y<.22):continue
    tex=sum((uv.data[i].uv for i in polygon.loop_indices),start=uv.data[polygon.loop_indices[0]].uv*0)/len(polygon.loop_indices)
    col=pixels[min(4095,max(0,int((1-tex.y)*4096))),min(4095,max(0,int(tex.x*4096)))]
    teal=col[1]>col[0]*1.12 and col[2]>col[0]*.95
    if not teal:selected.append(polygon.index)
vertices=[];faces=[];texcoords=[];lookup={}
for pi in selected:
    polygon=me.polygons[pi];face=[]
    for li in polygon.loop_indices:
        vi=me.loops[li].vertex_index
        if vi not in lookup:lookup[vi]=len(vertices);vertices.append(tuple(me.vertices[vi].co+me.vertices[vi].normal*.004))
        face.append(lookup[vi]);texcoords.append(tuple(uv.data[li].uv))
    faces.append(face)
armor_mesh=bpy.data.meshes.new('Separate copper shoulder shell');armor_mesh.from_pydata(vertices,[],faces);armor_mesh.update()
layer=armor_mesh.uv_layers.new(name='KeeperAtlas')
for i,tex in enumerate(texcoords):layer.data[i].uv=tex
armor=bpy.data.objects.new('Keeper rigid right pauldron',armor_mesh);bpy.context.collection.objects.link(armor);armor_mesh.materials.append(me.materials[0]);armor['rigid_bone']='ArmR'
for polygon in armor_mesh.polygons:polygon.use_smooth=True
bpy.context.view_layer.objects.active=armor
solid=armor.modifiers.new('Forged shell thickness','SOLIDIFY');solid.thickness=.012;solid.offset=0;solid.use_rim=True
bpy.ops.object.modifier_apply(modifier=solid.name)
# Retain a darker flexible sleeve behind the independent shell so rotation does
# not open a hole into the body. Its outline stays under the solid armor.
lining=bpy.data.materials.new('Leather | concealed shoulder sleeve');lining.diffuse_color=(.035,.027,.021,1);lining.use_nodes=True;lining.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=lining.diffuse_color;lining.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.9
me.materials.append(lining)
for pi in selected:me.polygons[pi].material_index=1
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Keeper-structured.blend'))
print('KEEPER_SHOULDER_SEPARATED',len(vertices),len(faces),flush=True)
