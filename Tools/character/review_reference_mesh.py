"""Render actual generated geometry from four views, plus an untextured check.

Run with the project's bpy Python runtime. These images are model renders;
the approved concept images are kept separately and never substituted here.
"""
from pathlib import Path
import argparse,json,math
import bpy
from mathutils import Vector,Matrix

ap=argparse.ArgumentParser()
ap.add_argument('mesh',type=Path)
ap.add_argument('--out',type=Path,required=True)
ap.add_argument('--rotate-x',type=float,default=0)
ap.add_argument('--rotate-z',type=float,default=0)
args=ap.parse_args()
args.out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(args.mesh.resolve()))
objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
rotation=Matrix.Rotation(math.radians(args.rotate_z),4,'Z')@Matrix.Rotation(math.radians(args.rotate_x),4,'X')
points=[rotation@o.matrix_world@v.co for o in objects for v in o.data.vertices]
low=Vector([min(v[i] for v in points)for i in range(3)])
high=Vector([max(v[i] for v in points)for i in range(3)])
center=(low+high)/2;center.z=low.z
scale=2.5/(high.z-low.z)
transform=Matrix.Scale(scale,4)@Matrix.Translation(-center)@rotation
statistics={'source':str(args.mesh),'source_bounds':[list(low),list(high)],'normalization_scale':scale,'objects':[]}
for o in objects:
    matrix=transform@o.matrix_world
    o.parent=None;o.matrix_world=Matrix.Identity(4)
    for v in o.data.vertices:v.co=matrix@v.co
    o.data.update();o.data.calc_loop_triangles()
    statistics['objects'].append({'name':o.name,'vertices':len(o.data.vertices),'triangles':len(o.data.loop_triangles),'materials':[m.name for m in o.data.materials],'uv_layers':len(o.data.uv_layers)})
(args.out/'geometry.json').write_text(json.dumps(statistics,indent=2))
for image in bpy.data.images:
    if image.packed_file:continue
    if image.source=='FILE':
        try:image.pack()
        except RuntimeError:pass
scene=bpy.context.scene
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.resolution_x=1050;scene.render.resolution_y=1250;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='Standard'
world=bpy.data.worlds.new('Review world');world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.15,.18,.19,1);world.node_tree.nodes['Background'].inputs[1].default_value=.65;scene.world=world
for name,pos,power,col,size in [('Key',(-3,-4,6),400,(1,.89,.78),5),('Fill',(3,-2,3),140,(.8,.9,1),4),('Rim',(2,3,4),360,(.8,.9,1),4)]:
    bpy.ops.object.light_add(type='AREA',location=pos);light=bpy.context.object;light.name=name;light.data.energy=power;light.data.color=col;light.data.shape='DISK';light.data.size=size;light.rotation_euler=(Vector((0,0,1.3))-light.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add();camera=bpy.context.object;camera.data.type='ORTHO';camera.data.ortho_scale=3.05;scene.camera=camera
def render(name,angle):
    target=Vector((0,0,1.25));camera.location=target+Vector((math.sin(angle)*5,-math.cos(angle)*5,.08));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str((args.out/(name+'.png')).resolve());bpy.ops.render.render(write_still=True)
for name,angle in [('front',0),('three-quarter',math.pi/5),('side',math.pi/2),('back',math.pi)]:render(name,angle)
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str((args.out/'candidate-review.blend').resolve()))
clay=bpy.data.materials.new('Plain clay geometry inspection');clay.diffuse_color=(.55,.54,.48,1);clay.use_nodes=True;bs=clay.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=clay.diffuse_color;bs.inputs['Roughness'].default_value=.85
for ob in objects:
    ob.data.materials.clear();ob.data.materials.append(clay)
    for p in ob.data.polygons:p.material_index=0
render('clay-three-quarter',math.pi/5)
# Exact cardinal cameras used to create the committed paint guides.
scene.render.resolution_x=scene.render.resolution_y=1254;scene.render.film_transparent=True;camera.data.ortho_scale=2.65
for name,angle in [('front',0),('back',math.pi),('left',math.pi/2),('right',-math.pi/2)]:
    target=Vector((0,0,1.25));camera.location=target+Vector((math.sin(angle)*5,-math.cos(angle)*5,0));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str((args.out/(name+'-clay.png')).resolve());bpy.ops.render.render(write_still=True)
print('GENERATED_GEOMETRY_REVIEW_READY',flush=True)
