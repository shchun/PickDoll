"""Rasterize the project's simple mascot shapes at 4x resolution; no external assets."""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1] / 'Assets/PickDoll/Art/Week13'
ROOT.mkdir(parents=True, exist_ok=True)
S = 4
INK = '#302933'
CREAM = '#fff8ed'
PINK = '#d893a5'

def mascot(background=None, monochrome=False):
    image = Image.new('RGBA', (1024*S, 1024*S), background or (0,0,0,0))
    d = ImageDraw.Draw(image)
    def ellipse(box, fill):
        d.ellipse(tuple(round(v*S) for v in box), fill=('#ffffff' if monochrome else fill))
    # All identity features stay within Android's adaptive icon safe circle.
    ellipse((332,183,448,502), INK)
    ellipse((565,166,678,493), INK)
    if not monochrome:
        ellipse((365,224,416,430), PINK)
        ellipse((596,209,645,421), PINK)
    ellipse((267,396,757,814), INK)
    ellipse((279,408,745,802), CREAM)
    if not monochrome:
        ellipse((335,464,454,663), INK)
        ellipse((570,464,689,663), INK)
        ellipse((379,510,405,551), CREAM)
        ellipse((618,510,644,551), CREAM)
        ellipse((337,650,420,688), PINK)
        ellipse((604,650,687,688), PINK)
        ellipse((491,638,533,665), PINK)
        d.line([(512*S,662*S),(512*S,683*S)], fill=INK, width=7*S)
        d.arc((475*S,660*S,513*S,702*S), 0, 160, fill=INK, width=7*S)
        d.arc((511*S,660*S,549*S,702*S), 20, 180, fill=INK, width=7*S)
    return image.resize((1024,1024), Image.Resampling.LANCZOS)

mascot().save(ROOT/'mascot.png')
mascot('#edd1d5').save(ROOT/'icon.png')
mascot(monochrome=True).save(ROOT/'icon-monochrome.png')
Image.new('RGBA',(1024,1024),'#edd1d5').save(ROOT/'icon-background.png')
mascot('#edd1d5').save(ROOT/'PickDoll.ico',sizes=[(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)])
