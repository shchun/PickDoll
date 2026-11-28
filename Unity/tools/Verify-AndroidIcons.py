"""Check the actual APK's adaptive icon layers, including every packaged density."""
import sys
from io import BytesIO
from zipfile import ZipFile
from PIL import Image

with ZipFile(sys.argv[1]) as apk:
    backgrounds = [n for n in apk.namelist() if n.startswith('res/mipmap-') and n.endswith('/ic_launcher_background.png')]
    assert backgrounds, 'No adaptive icon backgrounds found'
    for background in backgrounds:
        foreground = background.replace('ic_launcher_background.png', 'ic_launcher_foreground.png')
        bg = Image.open(BytesIO(apk.read(background))).convert('RGBA')
        fg = Image.open(BytesIO(apk.read(foreground))).convert('RGBA')
        assert bg.size == fg.size, background + ': layer sizes differ'
        assert bg.getchannel('A').getextrema() == (255, 255), background + ': background must be opaque'
        assert fg.getchannel('A').getextrema()[0] == 0, foreground + ': foreground has no transparent area'
        assert len(fg.getcolors(fg.width * fg.height) or []) > 32, foreground + ': foreground is missing the mascot artwork'
        print('PASS', foreground, fg.size)
    print('ADAPTIVE_ICON_LAYERS_PASS densities=' + str(len(backgrounds)))
