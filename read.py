import trimesh
import trimesh.scene
import numpy as np

# Yüklenecek model dosyaları
model_files = [
    './maps/map_0c6a10ae-6f9a-4a71-862c-9a0a913b1e35.obj',
    './maps/map_24c5f667-5eb7-443f-958b-a124dfa21b17.obj',
]

# Sahne oluştur
scene = trimesh.Scene()

# Modelleri sırayla yükle ve sahneye ekle
for i, file in enumerate(model_files):
    # MTL dosyasını manuel olarak belirt
    mesh = trimesh.load(file, process=True, mtl_name='./maps/material.mtl')
    mesh.apply_translation([i * 200.0, 0, 0])  # Çakışmasın diye x ekseninde kaydır
    scene.add_geometry(mesh)

# Görselleştir
scene.show()