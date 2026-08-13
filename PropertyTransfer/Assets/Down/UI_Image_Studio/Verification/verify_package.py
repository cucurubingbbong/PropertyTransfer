from pathlib import Path
import json, re, sys

root = Path(__file__).resolve().parents[1]
cs_files = sorted((root / 'Assets/Editor/UIImageStudio').glob('*.cs'))
required = {
    'UIStudioData.cs': ['class UIStudioLayer', 'class UIStudioDocumentData'],
    'UIStudioMath.cs': ['static float Snap', 'ClampToCanvas', 'FitRect'],
    'UIStudioExporter.cs': ['ExportPng', 'CreateRoundedRectTexture', 'LoadImageAsset'],
    'UIImageStudioWindow.cs': ['MenuItem("Tools/UI Image Studio', 'DrawWorkspace', 'DrawInspector', 'DrawLayersPanel', 'DrawAssetPanel'],
}

def strip_csharp(text: str) -> str:
    # Strip comments and string/char contents while preserving delimiter positions.
    out=[]; i=0; n=len(text); state='code'; verbatim=False
    while i<n:
        c=text[i]; nxt=text[i+1] if i+1<n else ''
        if state=='code':
            if c=='/' and nxt=='/': state='line'; out.extend('  '); i+=2; continue
            if c=='/' and nxt=='*': state='block'; out.extend('  '); i+=2; continue
            if c=='@' and nxt=='"': state='string'; verbatim=True; out.extend('  '); i+=2; continue
            if c=='"': state='string'; verbatim=False; out.append(' '); i+=1; continue
            if c=="'": state='char'; out.append(' '); i+=1; continue
            out.append(c); i+=1; continue
        if state=='line':
            if c=='\n': state='code'; out.append('\n')
            else: out.append(' ')
            i+=1; continue
        if state=='block':
            if c=='*' and nxt=='/': state='code'; out.extend('  '); i+=2
            else: out.append('\n' if c=='\n' else ' '); i+=1
            continue
        if state=='string':
            if verbatim:
                if c=='"' and nxt=='"': out.extend('  '); i+=2; continue
                if c=='"': state='code'; out.append(' '); i+=1; continue
            else:
                if c=='\\': out.extend('  ' if i+1<n else ' '); i+=2; continue
                if c=='"': state='code'; out.append(' '); i+=1; continue
            out.append('\n' if c=='\n' else ' '); i+=1; continue
        if state=='char':
            if c=='\\': out.extend('  ' if i+1<n else ' '); i+=2; continue
            if c=="'": state='code'; out.append(' '); i+=1; continue
            out.append(' '); i+=1
    return ''.join(out)

def check_balanced(path: Path):
    text=strip_csharp(path.read_text(encoding='utf-8'))
    pairs={')':'(',']':'[','}':'{'}; opens=set(pairs.values()); stack=[]
    line=1
    for c in text:
        if c=='\n': line+=1
        elif c in opens: stack.append((c,line))
        elif c in pairs:
            if not stack or stack[-1][0]!=pairs[c]:
                raise AssertionError(f'{path.name}:{line}: mismatched {c}')
            stack.pop()
    if stack:
        raise AssertionError(f'{path.name}: unclosed delimiters {stack[-5:]}')

for path in cs_files:
    check_balanced(path)
    text=path.read_text(encoding='utf-8')
    assert 'TODO' not in text and 'TBD' not in text and 'NotImplementedException' not in text, path.name

for name, needles in required.items():
    text=(root/'Assets/Editor/UIImageStudio'/name).read_text(encoding='utf-8')
    for needle in needles:
        assert needle in text, f'{name} missing {needle}'

sample=json.loads((root/'Examples/SampleLayout.json').read_text(encoding='utf-8'))
assert sample['canvasWidth']==1920 and sample['canvasHeight']==1080
assert len(sample['layers'])>=4
assert all('id' in layer and 'rect' in layer for layer in sample['layers'])

# Sanity-check the math examples mirrored by UIStudioMath.
def snap(v, step): return round(v/step)*step if step>0 else v
assert snap(30,16)==32
assert snap(22,16)==16

print(f'PASS: {len(cs_files)} C# files structurally checked')
print('PASS: required editor/export symbols found')
print('PASS: sample document JSON parsed')
print('NOTE: Unity Editor is not installed in this environment, so Unity compilation/EditMode tests cannot be executed here.')
