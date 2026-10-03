from pathlib import Path
import shutil
root=Path(__file__).resolve().parents[1]
out=root/'dist/pages'
if out.exists(): shutil.rmtree(out)
out.mkdir(parents=True)
for p in (root/'site').iterdir():
    if p.is_file(): shutil.copy2(p,out/p.name)
shutil.copytree(root/'public',out/'demo')
shutil.copytree(root/'images',out/'images')
(out/'.nojekyll').touch()
print('Pages: dist/pages')
