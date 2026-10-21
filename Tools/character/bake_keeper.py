"""Bake aligned reference painting onto a reduced, editable runtime mesh."""
from pathlib import Path
import bpy,math,json

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Characters'
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Build/CharacterReview/HunyuanPaint/keeper-painted-study.blend'))
high=next(o for o in bpy.context.scene.objects if o.type=='MESH')
high.name='Keeper high resolution projection source'
low=high.copy();low.data=high.data.copy();low.name='Keeper runtime body';bpy.context.collection.objects.link(low)
bpy.ops.object.select_all(action='DESELECT');low.select_set(True);bpy.context.view_layer.objects.active=low
mod=low.modifiers.new('Runtime surface reduction','DECIMATE');mod.ratio=180000/len(low.data.polygons);mod.use_collapse_triangulate=True
bpy.ops.object.modifier_apply(modifier=mod.name)
low.data.uv_layers.new(name='KeeperAtlas');low.data.uv_layers.active_index=len(low.data.uv_layers)-1
low.data.uv_layers.active.active_render=True
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.006,area_weight=.3,correct_aspect=True)
bpy.ops.object.mode_set(mode='OBJECT')
atlas=bpy.data.images.new('Keeper painted albedo',width=4096,height=4096,alpha=False)
atlas.colorspace_settings.name='sRGB'
material=high.data.materials[0].copy();material.name='Keeper projection bake'
nodes=material.node_tree.nodes;texture=nodes.new('ShaderNodeTexImage');texture.image=atlas;nodes.active=texture
low.data.materials.clear();low.data.materials.append(material)
for face in low.data.polygons:face.material_index=0
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=8
scene.render.bake.use_selected_to_active=False;scene.render.bake.margin=16;scene.render.bake.use_clear=True
# Bake the preserved projective UVs on the reduced surface itself. A ray cage
# can hit a nearby arm or the opposite side of the thin cloak and contaminate it.
bpy.ops.object.select_all(action='DESELECT');high.hide_render=True;low.select_set(True);bpy.context.view_layer.objects.active=low
print('KEEPER_ATLAS_BAKE_START',len(low.data.vertices),len(low.data.polygons),flush=True)
bpy.ops.object.bake(type='EMIT')
atlas.filepath_raw=str(OUT/'KeeperAlbedo.png');atlas.file_format='PNG';atlas.save();atlas.pack()
nodes.clear();output=nodes.new('ShaderNodeOutputMaterial');emission=nodes.new('ShaderNodeEmission');texture=nodes.new('ShaderNodeTexImage');texture.image=atlas
material.name='Keeper painted atlas';material.node_tree.links.new(texture.outputs['Color'],emission.inputs['Color']);material.node_tree.links.new(emission.outputs[0],output.inputs['Surface'])
for name in ['Projectfront','Projectback','Projectleft','Projectright']:
    low.data.uv_layers.remove(low.data.uv_layers[name])
while low.data.color_attributes:low.data.color_attributes.remove(low.data.color_attributes[0])
high_mesh=high.data;bpy.data.objects.remove(high,do_unlink=True);bpy.data.meshes.remove(high_mesh)
bpy.ops.object.select_all(action='DESELECT');low.select_set(True);bpy.context.view_layer.objects.active=low
scene.render.bake.use_selected_to_active=False
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Keeper.blend'))
(OUT/'Keeper-bake.json').write_text(json.dumps({'source':'Generated/HunyuanMLX/keeper-shape-candidate.glb','vertices':len(low.data.vertices),'triangles':len(low.data.polygons),'texture_size':4096,'uv_layers':len(low.data.uv_layers),'status':'unrigged candidate; visual review and deformation tests required'},indent=2))
print('KEEPER_ATLAS_BAKE_OK',flush=True)
