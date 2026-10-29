"""Original Costa Brava props. Run with Blender --background --python this_file."""
import bpy, math, pathlib, random
ROOT=pathlib.Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/CostaBrava/Art/Custom'; OUT.mkdir(parents=True,exist_ok=True)
SOURCE=ROOT/'SourceArt'; SOURCE.mkdir(exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
colors={'mint':(.12,.65,.49,1),'cream':(.94,.86,.64,1),'coral':(.92,.31,.19,1),'navy':(.035,.11,.16,1),'wood':(.48,.26,.12,1),'leaf':(.12,.46,.24,1),'gold':(1,.64,.17,1)}
mats={}
for name,color in colors.items():
 m=bpy.data.materials.new(name);m.diffuse_color=color;mats[name]=m
def cube(name,loc,scale,mat):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.scale=scale;o.data.materials.append(mats[mat]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return o
def text(value,loc,size,mat):
 bpy.ops.object.text_add(location=loc,rotation=(math.pi/2,0,0));o=bpy.context.object;o.name='Sign lettering';o.data.body=value;o.data.size=size;o.data.align_x='CENTER';o.data.extrude=.015;o.data.materials.append(mats[mat]);bpy.ops.object.convert(target='MESH');return o
def export(name,objects):
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0]
 bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',bake_anim=False)
def group(build):
 before=set(bpy.data.objects);build();return list(set(bpy.data.objects)-before)
def kiosk():
 cube('VIVA kiosk', (0,0,1.4),(5,3,2.8),'cream');cube('Shop opening',(0,-1.52,1.45),(3.8,.1,1.65),'navy')
 cube('Counter',(0,-1.9,.95),(4.1,.7,.25),'mint');cube('Awning',(0,-2.05,2.6),(5.6,1.3,.2),'mint')
 for x in [-2.4,2.4]:cube('Awning post',(x,-2.5,1.2),(.12,.12,2.4),'wood')
 cube('Brand fascia',(0,-1.56,3.3),(5.2,.2,.8),'mint');text('VIVA',(0,-1.69,3.03),.65,'cream')
 text('SUPRIMENTOS',(0,-1.71,2.76),.17,'cream')
def palm():
 bpy.ops.mesh.primitive_cone_add(vertices=9,radius1=.27,radius2=.13,depth=6,location=(0,0,3));bpy.context.object.name='Palm trunk';bpy.context.object.data.materials.append(mats['wood'])
 for i in range(7):
  a=i*math.tau/7;bpy.ops.mesh.primitive_uv_sphere_add(segments=8,ring_count=4,radius=1,location=(math.cos(a)*1.3,math.sin(a)*1.3,5.65));o=bpy.context.object;o.name='Palm leaf';o.scale=(2,.43,.13);o.rotation_euler=(0,.3,a);o.data.materials.append(mats['leaf'])
def bench():
 for y in [-.24,0,.24]:cube('Seat slat',(0,y,.55),(2.2,.18,.1),'wood')
 for z in [.82,1.07]:cube('Back slat',(0,.33,z),(2.2,.1,.19),'wood')
 for x in [-.85,.85]:cube('Bench legs',(x,0,.26),(.13,.48,.52),'navy');cube('Back supports',(x,.33,.76),(.13,.13,.65),'navy')
def sculpture():
 cube('Base',(0,0,.2),(2,2,.4),'cream')
 for i in range(5):
  o=cube('Wave',(math.sin(i*.9)*.45,0,.6+i*.5),(.4,1.1,.7),'mint');o.rotation_euler.y=math.sin(i*.8)*.7
 text('COSTA BRAVA',(0,-1.015,.15),.16,'navy')
for name,func in [('viva-kiosk',kiosk),('palm',palm),('bench',bench),('wave-monument',sculpture)]:
 objects=group(func);export(name,objects)
 # Keep all originals in one editable Blender library, arranged separately.
 for o in objects:o.location.x+=12*list(['viva-kiosk','palm','bench','wave-monument']).index(name)
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'CostaBrava_Props.blend'))
print('Costa Brava original art saved')
