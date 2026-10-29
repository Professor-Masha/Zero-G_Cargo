"""Render campaign sector 50 from the actual Blender assets and bundled layout, not Unity."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
scene=bpy.data.scenes.new('Campaign board preview');bpy.context.window.scene=scene
source=bpy.data.collections
shuttle_names=['ShuttleMagenta','ShuttleLime','ShuttleAmber','ShuttleViolet']
source_offsets={'ShuttleMagenta':(-2,1.4),'ShuttleLime':(-.65,1.4),'ShuttleAmber':(.7,1.4),'ShuttleViolet':(2.05,1.4),'Dock':(-2,-.1),'Meteorite':(-.65,-.1),'EnergyBarrier':(.8,-.1),'GravityWell':(2.2,-.1),'StationDeck':(0,0)}
colors=['Magenta','Lime','Amber','Violet']
def copy(name,pos,scale=1,angle=0,color=None):
 ox,oy=source_offsets[name]
 for item in source[name].objects:
  obj=item.copy();obj.data=item.data.copy();scene.collection.objects.link(obj);obj.hide_render=False
  local=item.location-Vector((ox,oy,0));local*=scale
  x=local.x*math.cos(angle)-local.y*math.sin(angle);y=local.x*math.sin(angle)+local.y*math.cos(angle)
  obj.location=Vector((x+pos[0],y+pos[1],local.z+pos[2]));obj.scale*=scale;obj.rotation_euler.z+=angle
  if color is not None and 'Cargo identification' in item.name:obj.data.materials.clear();obj.data.materials.append(bpy.data.materials[colors[color]])
level=json.loads((ROOT/'Assets/_Project/Resources/Levels/campaign.json').read_text())['levels'][49]
copy('StationDeck',(0,0,0))
for i,cargo in enumerate(level['cargo']):
 p=cargo['spawn'];b=level['bays'][i]['position'];angle=math.atan2(b['y']-p['y'],b['x']-p['x'])
 copy(shuttle_names[cargo['color']],(p['x'],p['y'],.07),.5,angle)
 copy('Dock',(b['x'],b['y'],-.015),color=cargo['color'])
for h in level['hazards']:
 name=['Meteorite','EnergyBarrier','GravityWell'][h['kind']];p=h['position'];copy(name,(p['x'],p['y'],0),h['radius']/.44 if h['kind']==0 else 1)
bpy.ops.object.light_add(type='AREA',location=(1,-3,7));bpy.context.object.data.energy=800;bpy.context.object.data.size=6
bpy.ops.object.light_add(type='AREA',location=(-4,3,4));bpy.context.object.data.energy=300;bpy.context.object.data.size=5;bpy.context.object.data.color=(.12,.6,1)
bpy.ops.object.camera_add(location=(0,-3.2,10));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,0))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=11.0;scene.camera=camera
scene.world=bpy.data.worlds.new('Quiet station world');scene.world.color=(.025,.035,.055)
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=False;scene.render.resolution_x=720;scene.render.resolution_y=1280;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='JPEG';scene.render.filepath=str(ROOT/'docs/visuals/sector-50-blender-preview.jpg')
bpy.ops.render.render(write_still=True)
