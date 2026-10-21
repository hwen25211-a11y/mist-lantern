"""Project approved front/rear color references onto actual 3D geometry.

This is a DCC vertex-paint study, not a replacement of the mesh by a billboard.
Source PNGs remain unchanged. Fine detail and reference alignment are reviewed
before any runtime mesh reduction or binding.
"""
from pathlib import Path
import json,math
import bpy
import numpy as np
from PIL import Image
from scipy.ndimage import distance_transform_edt
from scipy.sparse import coo_matrix,diags
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Build/CharacterReview/HunyuanPaint'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Build/CharacterReview/HunyuanMLX/candidate-review.blend'))
def reference(name):
    pixels=np.array(Image.open(ROOT/'ArtSource/Characters/Concept'/name).convert('RGBA'))/255.
    mask=pixels[:,:,3]>.5
    yy,xx=np.where(mask)
    nearest=distance_transform_edt(~mask,return_distances=False,return_indices=True)
    return pixels,nearest,(xx.min(),yy.min(),xx.max(),yy.max())
front=reference('keeper-paint-front-v1.png');back=reference('keeper-paint-back-v1.png')
left=reference('keeper-paint-left-v1.png');right=reference('keeper-paint-right-v1.png')
def projection(positions,view,size=1254):
    scale=size/2.65
    axis=positions[:,0] if view<2 else positions[:,1]
    sign=-1 if view in [1,3] else 1
    x=(size-1)/2+axis*scale*sign
    y=(size-1)/2-(positions[:,2]-1.25)*scale
    return x,y
def sample(reference,positions,view):
    pixels,nearest,box=reference
    x,y=projection(positions,view,pixels.shape[0])
    x=np.clip(np.rint(x).astype(int),0,pixels.shape[1]-1);y=np.clip(np.rint(y).astype(int),0,pixels.shape[0]-1)
    confidence=np.where(pixels[y,x,3]>.5,1,.02)
    y,x=nearest[:,y,x]
    rgb=pixels[y,x,:3]
    return np.where(rgb<=.04045,rgb/12.92,((rgb+.055)/1.055)**2.4),confidence
mat=bpy.data.materials.new('Four-view aligned projected paint');mat.use_nodes=True
nodes=mat.node_tree.nodes;nodes.clear();output=nodes.new('ShaderNodeOutputMaterial');shader=nodes.new('ShaderNodeEmission');color=nodes.new('ShaderNodeVertexColor');color.layer_name='ReferencePaint'
links=mat.node_tree.links
weight_node=nodes.new('ShaderNodeVertexColor');weight_node.layer_name='ViewWeights'
split=nodes.new('ShaderNodeSeparateColor');links.new(weight_node.outputs['Color'],split.inputs[0])
weight_sockets=[split.outputs[i]for i in range(3)]+[weight_node.outputs['Alpha']]
combined=None
for view,name in enumerate(['front','back','left','right']):
    uv=nodes.new('ShaderNodeUVMap');uv.uv_map='Project'+name
    texture=nodes.new('ShaderNodeTexImage');texture.image=bpy.data.images.load(str(ROOT/'ArtSource/Characters/Concept'/('keeper-paint-'+name+'-v1.png')));texture.image.pack();texture.extension='EXTEND';links.new(uv.outputs['UV'],texture.inputs['Vector'])
    fallback=nodes.new('ShaderNodeMixRGB');links.new(texture.outputs['Alpha'],fallback.inputs[0]);links.new(color.outputs['Color'],fallback.inputs[1]);links.new(texture.outputs['Color'],fallback.inputs[2])
    mult=nodes.new('ShaderNodeMixRGB');mult.blend_type='MULTIPLY';mult.inputs[0].default_value=1;links.new(fallback.outputs[0],mult.inputs[1]);links.new(weight_sockets[view],mult.inputs[2])
    if combined is None:combined=mult.outputs[0]
    else:
        add=nodes.new('ShaderNodeMixRGB');add.blend_type='ADD';add.inputs[0].default_value=1;links.new(combined,add.inputs[1]);links.new(mult.outputs[0],add.inputs[2]);combined=add.outputs[0]
repair=nodes.new('ShaderNodeVertexColor');repair.layer_name='OcclusionRepair'
repaired=nodes.new('ShaderNodeMixRGB');links.new(repair.outputs['Alpha'],repaired.inputs[0]);links.new(combined,repaired.inputs[1]);links.new(repair.outputs['Color'],repaired.inputs[2])
surface=nodes.new('ShaderNodeVertexColor');surface.layer_name='LeatherSurface'
channels=nodes.new('ShaderNodeSeparateColor');links.new(repaired.outputs[0],channels.inputs[0])
def math_node(operation,a,b):
    node=nodes.new('ShaderNodeMath');node.operation=operation
    for socket,value in zip(node.inputs,[a,b]):
        if isinstance(value,(float,int)):socket.default_value=value
        else:links.new(value,socket)
    return node.outputs[0]
is_green=math_node('GREATER_THAN',channels.outputs[1],math_node('MULTIPLY',channels.outputs[0],1.12))
is_blue=math_node('GREATER_THAN',channels.outputs[2],math_node('MULTIPLY',channels.outputs[0],.95))
leather_mask=math_node('MULTIPLY',surface.outputs['Alpha'],math_node('MULTIPLY',is_green,is_blue))
glove_repair=nodes.new('ShaderNodeMixRGB');links.new(leather_mask,glove_repair.inputs[0]);links.new(repaired.outputs[0],glove_repair.inputs[1]);glove_repair.inputs[2].default_value=(.045,.033,.027,1)
cloth_surface=nodes.new('ShaderNodeVertexColor');cloth_surface.layer_name='ClothSurface'
warm=math_node('GREATER_THAN',channels.outputs[0],math_node('MULTIPLY',channels.outputs[1],1.10))
light=math_node('GREATER_THAN',channels.outputs[0],.09)
cloth_mask=math_node('MULTIPLY',cloth_surface.outputs['Alpha'],math_node('MULTIPLY',warm,light))
cloth_repair=nodes.new('ShaderNodeMixRGB');links.new(cloth_mask,cloth_repair.inputs[0]);links.new(glove_repair.outputs[0],cloth_repair.inputs[1]);links.new(cloth_surface.outputs['Color'],cloth_repair.inputs[2])
links.new(cloth_repair.outputs[0],shader.inputs['Color']);links.new(shader.outputs[0],output.inputs['Surface'])
for ob in [o for o in bpy.context.scene.objects if o.type=='MESH']:
    me=ob.data;positions=np.empty(len(me.vertices)*3);normals=np.empty_like(positions);me.vertices.foreach_get('co',positions);me.vertices.foreach_get('normal',normals);positions=positions.reshape(-1,3);normals=normals.reshape(-1,3)
    tree=BVHTree.FromObject(ob,bpy.context.evaluated_depsgraph_get())
    all_colors=[];all_weights=[]
    for view,(ref,direction,priority) in enumerate(zip([front,back,left,right],[(0,-1,0),(0,1,0),(1,0,0),(-1,0,0)],[1.8,1.2,1,1])):
        rgb,confidence=sample(ref,positions,view);all_colors.append(rgb)
        dot=np.maximum(0,normals@np.array(direction));weights=dot**8*confidence*priority
        direction=Vector(direction)
        for i in np.flatnonzero(weights>1e-7):
            hit=tree.ray_cast(Vector(positions[i])+direction*3,-direction,3.02)
            if hit[0] is not None and hit[3]<2.991:weights[i]=0
        all_weights.append(weights)
        print('PAINT_VISIBLE_VIEW',view,int(np.count_nonzero(weights)),flush=True)
    all_colors=np.stack(all_colors);weights=np.stack(all_weights)
    rear_cloth=all_colors[1]
    cape=(positions[:,1]>.17)&(positions[:,2]>.58)&(positions[:,2]<1.90)&(np.abs(positions[:,0])<.68)&(rear_cloth[:,1]>rear_cloth[:,0]*1.2)&(rear_cloth[:,2]>rear_cloth[:,0]*.9)
    weights[:,cape]=0;weights[1,cape]=1
    zero=weights.sum(0)<1e-10
    if zero.any():
        # Hidden surfaces receive the nearest angular reference, not arbitrary
        # colors from another part that occludes them in the image.
        fallback=np.argmax(np.abs(normals[zero,:2]),axis=1)
        choice=np.where(fallback==0,np.where(normals[zero,0]>0,2,3),np.where(normals[zero,1]>0,1,0))
        weights[choice,np.flatnonzero(zero)]=1
    colors=(all_colors*weights[:,:,None]).sum(0)/weights.sum(0)[:,None]
    # Inpaint occluded mesh surfaces through connected neighbors. Projecting an
    # unseen palm/armpit directly samples the cloak or sleeve in front of it.
    edges=np.empty(len(me.edges)*2,dtype=np.int32);me.edges.foreach_get('vertices',edges);edges=edges.reshape(-1,2)
    rows=np.concatenate([edges[:,0],edges[:,1]]);cols=np.concatenate([edges[:,1],edges[:,0]])
    adj=coo_matrix((np.ones(len(rows)),(rows,cols)),shape=(len(positions),len(positions))).tocsr();adj=diags(1/np.maximum(np.asarray(adj.sum(1)).ravel(),1))@adj
    repaired_colors=colors.copy()
    for _ in range(64):repaired_colors[zero]=(adj@repaired_colors)[zero]
    correction=np.column_stack([repaired_colors,zero.astype(float)])
    x=np.abs(positions[:,0]);y=positions[:,1];z=positions[:,2]
    teal=(colors[:,1]>colors[:,0]*1.12)&(colors[:,2]>colors[:,0]*.95)
    glove=(x>.57)&(z<1.10)&(z>.62)&(y<.19)&teal
    armpit=(x>.29)&(x<.56)&(z>1.02)&(z<1.42)&(y>-.08)&(y<.18)&(colors[:,0]>.15)&(colors[:,1]>.10)
    # These masks describe actual leather surfaces, not changes to the guides.
    for mask,srgb in [(glove,[.25,.205,.18]),(armpit,[.24,.20,.18])]:
        base=np.array(srgb);base=np.where(base<=.04045,base/12.92,((base+.055)/1.055)**2.4)
        correction[mask,:3]=base[None,:]*(.86+.18*np.abs(normals[mask,1,None]));correction[mask,3]=1
    correction_attr=me.color_attributes.new(name='OcclusionRepair',type='FLOAT_COLOR',domain='POINT');correction_attr.data.foreach_set('color',correction.astype(np.float32).ravel())
    leather=np.ones((len(positions),4),dtype=np.float32);leather[:,3]=((x>.56)&(z<1.06)&(z>.60)&(y<.20)).astype(float)
    leather_attr=me.color_attributes.new(name='LeatherSurface',type='FLOAT_COLOR',domain='POINT');leather_attr.data.foreach_set('color',leather.ravel())
    cloth=np.ones((len(positions),4),dtype=np.float32);cloth[:,:3]=np.array([.040,.085,.090])[None,:]*(.8+.3*np.abs(normals[:,1,None]));cloth[:,3]=((x>.40)&(z<1.45)&(z>.35)&(y>.19)).astype(float)
    cloth_attr=me.color_attributes.new(name='ClothSurface',type='FLOAT_COLOR',domain='POINT');cloth_attr.data.foreach_set('color',cloth.ravel())
    print('PAINT_OCCLUSION_REPAIR',int(zero.sum()),'glove',int(glove.sum()),'armpit',int(armpit.sum()),flush=True)
    normalized=(weights/weights.sum(0)[None,:]).T
    weights_attribute=me.color_attributes.new(name='ViewWeights',type='FLOAT_COLOR',domain='POINT');weights_attribute.data.foreach_set('color',normalized.astype(np.float32).ravel())
    loops=np.empty(len(me.loops),dtype=np.int32);me.loops.foreach_get('vertex_index',loops)
    for view,name in enumerate(['front','back','left','right']):
        x,y=projection(positions,view)
        uv_values=np.column_stack([x/1254,1-y/1254]).astype(np.float32)
        layer=me.uv_layers.new(name='Project'+name);layer.data.foreach_set('uv',uv_values[loops].ravel())
    colors=np.column_stack([colors,np.ones(len(colors))])
    attribute=me.color_attributes.new(name='ReferencePaint',type='FLOAT_COLOR',domain='POINT');attribute.data.foreach_set('color',colors.astype(np.float32).ravel())
    me.materials.clear();me.materials.append(mat)
    for face in me.polygons:face.material_index=0
scene=bpy.context.scene;scene.cycles.samples=16
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'keeper-painted-study.blend'))
camera=scene.camera
for name,angle in [('front',0),('three-quarter',math.pi/5),('side',math.pi/2),('back',math.pi)]:
    target=Vector((0,0,1.25));camera.location=target+Vector((math.sin(angle)*5,-math.cos(angle)*5,.08));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
print('REFERENCE_COLOR_STUDY_READY',flush=True)
