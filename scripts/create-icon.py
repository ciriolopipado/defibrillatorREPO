"""Generate the original package icon; requires Pillow, not used by the game build."""
from pathlib import Path
from PIL import Image, ImageDraw

image = Image.new("RGB", (256, 256), "#171e23")
draw = ImageDraw.Draw(image)
draw.rounded_rectangle((29, 66, 227, 208), radius=14, fill="#d86a20", outline="#f49c42", width=4)
draw.rounded_rectangle((84, 32, 172, 82), radius=12, outline="#67747a", width=13)
draw.rectangle((29, 184, 227, 207), fill="#30383c")
draw.rounded_rectangle((48, 90, 145, 151), radius=5, fill="#0e3638")
draw.line([(55, 124), (77, 124), (86, 107), (96, 141), (106, 115), (115, 124), (135, 124)], fill="#68f5be", width=4)
draw.rectangle((168, 101, 205, 114), fill="#eee8d2")
draw.rectangle((180, 89, 193, 126), fill="#eee8d2")
for index in range(4):
    draw.rounded_rectangle((51 + index * 24, 164, 68 + index * 24, 176), radius=2, fill="#65dd86")
draw.rounded_rectangle((9, 101, 31, 168), radius=5, fill="#a9b3b7", outline="#343e43", width=3)
draw.rounded_rectangle((225, 101, 247, 168), radius=5, fill="#a9b3b7", outline="#343e43", width=3)
draw.arc((10, 149, 66, 235), 10, 180, fill="#89969d", width=5)
draw.arc((190, 149, 246, 235), 0, 170, fill="#89969d", width=5)
image.save(Path(__file__).resolve().parents[1] / "thunderstore/icon.png")
