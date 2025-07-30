import trimesh

scene = trimesh.Scene()
loaded_scene = trimesh.load('maps/map_c4696336-a783-46c9-b114-5c4bf500de2e.glb')
scene.add_geometry(loaded_scene)

# Step 5: Show the loaded scene
if isinstance(scene, trimesh.Scene):
    scene.show()
else:
    trimesh.Scene(scene).show()
