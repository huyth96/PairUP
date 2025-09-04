import os

# Danh sách folder cần có trong Assets
folders = [
    "Assets/Art",
    "Assets/Audio",
    "Assets/Prefabs",
    "Assets/Scenes",
    "Assets/Scripts",
    "Assets/ScriptableObjects",
    "Assets/Materials",
    "Assets/Settings"
]

for folder in folders:
    os.makedirs(folder, exist_ok=True)
    print(f"✅ Created: {folder}")
