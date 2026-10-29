"""Original Zero-G Cargo assets. Run: blender -b -t 2 -P art-source/build_models.py"""
import bpy,math,random
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]; OUT=ROOT/'Assets/_Project/Resources/Models'; OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def mat(name,color,metal=.0,rough=.5,emission=0):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
 if emission:p.inputs['Emission Color'].default_value=(*color,1);p.inputs['Emission Strength'].default_value=emission
 return m
hull=mat('Hull',(.055,.085,.14),.7,.32);edge=mat('Steel',(.16,.23,.32),.65,.3);dark=mat('Deck',(.018,.03,.055),.5,.65);glass=mat('Glass',(.025,.16,.22),.4,.2);cyan=mat('Cyan',(0,.82,1),.3,.3,2);red=mat('Danger',(1,.07,.07),.2,.4,1.5);rock=mat('Rock',(.14,.18,.23),.25,.8)
colors=[mat('Magenta',(1,.025,.42),.3,.3,1.2),mat('Lime',(.5,1,.025),.3,.3,1.2),mat('Amber',(1,.46,.025),.3,.3,1.2),mat('Violet',(.44,.08,1),.3,.3,1.2)]
assetObjects=[]
def add(obj,material):obj.data.materials.append(material);assetObjects.append(obj);return obj
def cube(name,loc,scale,material,bevel=.02):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);add(o,material)
 if bevel:
  mod=o.modifiers.new('Machined edges','BEVEL');mod.width=bevel;mod.segments=2;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
 return o
def cyl(name,loc,r,depth,material,vertices=24):
 bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=depth,location=loc);o=bpy.context.object;o.name=name;return add(o,material)
def torus(name,loc,r,thick,material):
 bpy.ops.mesh.primitive_torus_add(major_segments=48,minor_segments=6,location=loc,major_radius=r,minor_radius=thick);o=bpy.context.object;o.name=name;return add(o,material)
def save_asset(name):
 bpy.ops.object.select_all(action='DESELECT')
 for o in assetObjects:o.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='AUTO')
 group=bpy.data.collections.new(name);bpy.context.scene.collection.children.link(group)
 for o in list(assetObjects):
  for col in list(o.users_collection):col.objects.unlink(o)
  group.objects.link(o)
 assetObjects.clear();return group
collections=[]
for i,color in enumerate(colors):
 verts=[(-.37,-.19,.07),(-.37,.19,.07),(.43,0,.07),(-.25,-.13,.22),(-.25,.13,.22),(.30,0,.14)]
 faces=[(0,2,1),(0,1,4,3),(1,2,5,4),(2,0,3,5),(3,4,5)]
 mesh=bpy.data.meshes.new('Wedge hull mesh');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Cargo shuttle hull',mesh);bpy.context.collection.objects.link(o);add(o,hull)
 for y in [-.18,.18]:
  cube('Swept wing',(-.16,y,.09),(.45,.20,.055),edge)
  engine=cyl('Ion engine',(-.36,y,.11),.065,.16,edge,16);engine.rotation_euler[1]=math.pi/2
  exhaust=cyl('Ion exhaust',(-.45,y,.11),.046,.018,cyan,16);exhaust.rotation_euler[1]=math.pi/2
 cube('Cockpit',(.04,0,.185),(.25,.15,.08),glass,.035)
 crystal=cyl('Cargo crystal',(-.18,0,.29),.10,.16,color,4);crystal.rotation_euler[2]=math.pi/4
 for y in [-.215,.215]:cube('Cargo stripe',(-.06,y,.125),(.25,.025,.025),color,.006)
 collections.append(save_asset('Shuttle'+['Magenta','Lime','Amber','Violet'][i]))
cyl('Hexagonal dock base',(0,0,.025),.54,.05,hull,6)
torus('Dock guide ring',(0,0,.055),.44,.014,cyan)
for i in range(6):
 a=i*math.pi/3
 post=cube('Dock coupling',(.46*math.cos(a),.46*math.sin(a),.08),(.12,.12,.08),edge,.015);post.rotation_euler[2]=a
 cube('Dock status light',(.46*math.cos(a),.46*math.sin(a),.135),(.045,.045,.018),cyan,.004)
cyl('Cargo identification',(0,0,.055),.14,.03,colors[0],6)
collections.append(save_asset('Dock'))
bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=.44,location=(0,0,.15));o=bpy.context.object;o.name='Irregular meteorite';random.seed(41)
for v in o.data.vertices:v.co*=random.uniform(.80,1.17)
add(o,rock)
for i in range(5):
 a=i*1.93;cube('Mineral seam',(.25*math.cos(a),.25*math.sin(a),.35),(.12,.025,.018),edge,.005)
collections.append(save_asset('Meteorite'))
for x in [-.5,.5]:
 cube('Emitter plinth',(x,0,.04),(.16,.25,.08),hull)
 cube('Energy emitter',(x,0,.20),(.09,.11,.32),edge)
 cube('Emitter glow',(x,0,.38),(.07,.09,.025),red,.005)
cube('Repulsor energy sheet',(0,0,.16),(1,.085,.18),red,.008)
collections.append(save_asset('EnergyBarrier'))
cyl('Gravity generator',(0,0,.035),.22,.07,hull,24)
for rr in [.30,.48,.68]:torus('Field ring',(0,0,.06),rr,.009,cyan)
cyl('Gravity lens',(0,0,.09),.10,.06,cyan,24)
collections.append(save_asset('GravityWell'))
cube('Station deck',(0,0,-.12),(6.4,7.8,.14),dark,.03)
# Subtle etched panel seams and edge rails, leaving the board open.
for x in [-3,-2,-1,0,1,2,3]:cube('Deck seam',(x,0,-.043),(.008,7.5,.004),hull,0)
for y in [-3,-2,-1,0,1,2,3]:cube('Deck seam',(0,y,-.043),(6.2,.008,.004),hull,0)
for side in [-1,1]:
 cube('Station edge rail',(side*3.14,0,.0),(.12,7.65,.12),edge)
 for y in [-3.25,-2.2,-1.1,0,1.1,2.2,3.25]:
  cube('Edge luminaire',(side*3.08,y,.07),(.035,.52,.02),cyan,.005)
  cube('Service panel',(side*2.94,y,.0),(.17,.35,.07),hull)
 for x in [-2.5,-1.5,0,1.5,2.5]:
  cube('Airlock bulkhead',(x,side*3.77,.02),(.65,.17,.15),edge)
  cube('Bulkhead strip',(x,side*3.67,.105),(.4,.025,.015),cyan,.002)
collections.append(save_asset('StationDeck'))
# Save an editable source scene with assets arranged as a presentation, then render a true 3D preview.
positions=[(-2,1.4),(-.65,1.4),(.7,1.4),(2.05,1.4),(-2,-.1),(-.65,-.1),(.8,-.1),(2.2,-.1)]
for col,(x,y) in zip(collections[:8],positions):
 for obj in col.objects:obj.location.x+=x;obj.location.y+=y
# Deck shown separately behind assets; shrink to presentation board size.
for obj in collections[8].objects:obj.hide_render=True
cube('Presentation floor',(0,.4,-.40),(6.4,4.3,.15),dark,.08)
world=bpy.context.scene.world;world.color=(.04,.04,.04)
bpy.ops.object.light_add(type='AREA',location=(1,-3,6));bpy.context.object.data.energy=850;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=5
bpy.ops.object.light_add(type='AREA',location=(-4,2,3));bpy.context.object.data.energy=450;bpy.context.object.data.color=(.12,.6,1);bpy.context.object.data.size=4
bpy.ops.object.camera_add(location=(3,-5.8,6.8));camera=bpy.context.object;camera.rotation_euler=(Vector((0,.5,0))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=7.4;bpy.context.scene.camera=camera
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=False;scene.render.resolution_x=1440;scene.render.resolution_y=1000;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='JPEG';scene.render.filepath=str(ROOT/'docs/visuals/3d-models-preview.jpg')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'art-source/ZeroGCargo-Models.blend'))
bpy.ops.render.render(write_still=True)
