"""Extend the original Blender prop library without replacing it."""
import bpy,math,pathlib
ROOT=pathlib.Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/CostaBrava/Art/Custom';OUT.mkdir(parents=True,exist_ok=True)
source=ROOT/'SourceArt/CostaBrava_Props.blend'
if source.exists():bpy.ops.wm.open_mainfile(filepath=str(source))
else:
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
palette={'navy':(.035,.11,.16,1),'cream':(.94,.86,.64,1),'coral':(.92,.31,.19,1),'mint':(.12,.65,.49,1),'wood':(.48,.26,.12,1),'metal':(.09,.13,.16,1),'glass':(.2,.65,.85,1)}
materials={}
for name,color in palette.items():
 m=bpy.data.materials.get(name) or bpy.data.materials.new(name);m.diffuse_color=color;materials[name]=m
def cube(name,loc,size,material):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.scale=size;o.data.materials.append(materials[material]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return o
def cylinder(name,loc,radius,depth,material,rotation=(0,0,0)):
 bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=radius,depth=depth,location=loc,rotation=rotation);o=bpy.context.object;o.name=name;o.data.materials.append(materials[material]);return o
def text(value,loc,size):
 bpy.ops.object.text_add(location=loc,rotation=(math.pi/2,0,0));o=bpy.context.object;o.data.body=value;o.data.align_x='CENTER';o.data.size=size;o.data.extrude=.01;o.data.materials.append(materials['cream']);bpy.ops.object.convert(target='MESH')
def shop():
 cube('Farol shop',(0,0,1.8),(9,7,3.6),'cream');cube('Entrance',(0,-3.51,1.35),(3.4,.1,2.7),'navy')
 cube('Roof',(0,0,3.7),(9.6,7.6,.25),'navy');cube('Brand fascia',(0,-3.6,3.1),(9.3,.2,1.05),'coral')
 text('FAROL / ARMAS',(0,-3.73,2.95),.58)
 for x in [-3.15,3.15]:
  cube('Display window',(x,-3.56,1.5),(1.8,.12,1.6),'glass');cube('Window grid',(x,-3.67,1.5),(.09,.1,1.6),'navy')
 cube('Counter',(0,-4.3,1.0),(3.5,.7,.3),'wood');text('RIFLE + LUNETA  $100',(0,-4.68,.86),.19)
def rifle():
 cube('Receiver',(0,-.2,0),(.095,.43,.11),'metal');cube('Stock',(0,.15,-.015),(.085,.3,.14),'wood')
 cube('Grip',(0,-.06,-.105),(.075,.09,.19),'metal');cube('Magazine',(0,-.26,-.12),(.055,.1,.2),'metal')
 cylinder('Long barrel',(0,-.6,.03),.025,.45,'metal',(math.pi/2,0,0))
 cylinder('Scope housing',(0,-.23,.15),.055,.27,'metal',(math.pi/2,0,0))
 cylinder('Scope lens',(0,-.372,.15),.046,.012,'glass',(math.pi/2,0,0))
 for y in [-.13,-.31]:cube('Scope mounts',(0,y,.09),(.05,.03,.12),'metal')
def barrel():
 cylinder('Barrel',(0,0,.6),.4,1.2,'coral')
 for z in [.18,.98]:cylinder('Steel band',(0,0,z),.415,.07,'metal')
def export(name,build,x):
 before=set(bpy.data.objects);build();objects=list(set(bpy.data.objects)-before)
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0]
 bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False)
 for o in objects:o.location.x+=x
export('farol-arms',shop,48);export('farol-rifle',rifle,61);export('breakable-barrel',barrel,65)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'SourceArt/CostaBrava_Expansion.blend'))
print('Farol shop, scoped rifle and breakable barrel exported.')
