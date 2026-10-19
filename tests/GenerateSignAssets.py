from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

assets = Path(__file__).resolve().parents[1] / "src/ValheimNamekeeper/Assets"
for locale, text in [("ru", "Лавка имён"), ("en", "Namekeeper")]:
    for size in range(240, 30, -1):
        font = ImageFont.truetype(str(assets / "MarckScript-Regular.ttf"), size)
        box = font.getbbox(text)
        if box[2]-box[0] <= 928 and box[3]-box[1] <= 184:
            break
    image = Image.new("RGBA", (1024, 256), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    x = (1024-(box[2]-box[0]))/2-box[0]
    y = (256-(box[3]-box[1]))/2-box[1]
    draw.text((x,y), text, font=font, fill=(21,17,13,255), stroke_width=1)
    image.save(assets / ("sign_"+locale+".png"), optimize=True)
    print(locale, "font size", size, "bounds", image.getbbox())
