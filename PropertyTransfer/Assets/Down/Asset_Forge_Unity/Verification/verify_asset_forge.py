from pathlib import Path
import json
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
CS_ROOT = ROOT / "Assets" / "Editor" / "AssetForge"

required = [
    CS_ROOT / "Editor2D" / "AF2DData.cs",
    CS_ROOT / "Editor2D" / "AF2DMath.cs",
    CS_ROOT / "Editor2D" / "AF2DExporter.cs",
    CS_ROOT / "Editor2D" / "AssetForge2DWindow.cs",
    CS_ROOT / "Editor3D" / "AF3DData.cs",
    CS_ROOT / "Editor3D" / "AF3DMath.cs",
    CS_ROOT / "Editor3D" / "AF3DPrimitiveFactory.cs",
    CS_ROOT / "Editor3D" / "AF3DExporter.cs",
    CS_ROOT / "Editor3D" / "AssetForge3DWindow.cs",
    ROOT / "Examples" / "Sample2DLayout.json",
    ROOT / "Examples" / "Sample3DModel.af3d.json",
    ROOT / "README.md",
]

for path in required:
    assert path.exists(), f"Missing required file: {path.relative_to(ROOT)}"
    assert path.stat().st_size > 0, f"Empty required file: {path.relative_to(ROOT)}"

# JSON examples must parse.
for path in [ROOT / "Examples" / "Sample2DLayout.json", ROOT / "Examples" / "Sample3DModel.af3d.json"]:
    data = json.loads(path.read_text(encoding="utf-8"))
    assert isinstance(data, dict), f"Example is not a JSON object: {path.name}"

sample3d = json.loads((ROOT / "Examples" / "Sample3DModel.af3d.json").read_text(encoding="utf-8"))
assert len(sample3d.get("parts", [])) >= 3, "3D sample should contain multiple primitive parts"

# Basic delimiter validation after removing comments and strings.
def stripped(source: str) -> str:
    source = re.sub(r'@"(?:[^"]|"")*"', '""', source, flags=re.S)
    source = re.sub(r'"(?:\\.|[^"\\])*"', '""', source, flags=re.S)
    source = re.sub(r'//.*', '', source)
    source = re.sub(r'/\*.*?\*/', '', source, flags=re.S)
    return source

for path in CS_ROOT.rglob("*.cs"):
    source = stripped(path.read_text(encoding="utf-8"))
    for opening, closing in [("{", "}"), ("(", ")"), ("[", "]")]:
        depth = 0
        for char in source:
            if char == opening:
                depth += 1
            elif char == closing:
                depth -= 1
                assert depth >= 0, f"Unexpected {closing} in {path.relative_to(ROOT)}"
        assert depth == 0, f"Unbalanced {opening}{closing} in {path.relative_to(ROOT)}"

all_source = "\n".join(path.read_text(encoding="utf-8") for path in CS_ROOT.rglob("*.cs"))
window3d = (CS_ROOT / "Editor3D" / "AssetForge3DWindow.cs").read_text(encoding="utf-8")
export3d = (CS_ROOT / "Editor3D" / "AF3DExporter.cs").read_text(encoding="utf-8")
data3d = (CS_ROOT / "Editor3D" / "AF3DData.cs").read_text(encoding="utf-8")
factory3d = (CS_ROOT / "Editor3D" / "AF3DPrimitiveFactory.cs").read_text(encoding="utf-8")

assert 'MenuItem("Tools/Asset Forge/2D Editor' in all_source
assert 'MenuItem("Tools/Asset Forge/3D Builder' in all_source
legacy_namespace = "namespace " + "UI" + "ImageStudio"
legacy_menu = "Tools/" + "UI Image" + " Studio"
assert legacy_namespace not in all_source
assert legacy_menu not in all_source

for primitive in ["Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Cone"]:
    assert primitive in data3d, f"Missing primitive enum: {primitive}"

for symbol in ["Handles.PositionHandle", "Handles.RotationHandle", "Handles.ScaleHandle", "Handles.DrawCamera", "EditorSceneManager.NewPreviewScene"]:
    assert symbol in window3d, f"Missing 3D viewport feature: {symbol}"

for symbol in ["PrefabUtility.SaveAsPrefabAsset", "CombineMeshes", "RenderPng"]:
    assert symbol in export3d, f"Missing exporter feature: {symbol}"

assert "AF3DPreviewLink" not in factory3d, "Editor-only preview components must not be attached to exportable objects"
assert "EditorApplication.update" not in window3d, "3D builder should not keep a permanent editor update loop"
assert "Event.current.type == EventType.Repaint" in window3d, "Camera drawing should be repaint-only"

print("Asset Forge static verification passed.")
print(f"C# files checked: {len(list(CS_ROOT.rglob('*.cs')))}")
print("Examples parsed: 2")
