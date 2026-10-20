"""Generates Knockback Rumble UI art (logo, 9-slice skins, icons) as PNGs.

Run:  python3 -I make_ui_art.py <fonts_dir> <out_dir>
Everything is drawn at 4x and downsampled for smooth edges.
"""
import math
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageChops

FONTS = Path(sys.argv[1])
OUT = Path(sys.argv[2])
OUT.mkdir(parents=True, exist_ok=True)
SS = 4  # supersample

INK = (20, 18, 28, 255)
YELLOW = (255, 201, 40, 255)
ORANGE = (255, 107, 26, 255)
RED = (230, 57, 70, 255)
CREAM = (255, 244, 214, 255)


def font(name, size):
    return ImageFont.truetype(str(FONTS / name), size)


def down(img, size):
    return img.resize(size, Image.LANCZOS)


def save(img, name):
    img.save(OUT / name)
    print("wrote", name, img.size)


# ---------------------------------------------------------------------------
# 9-slice skins. Grayscale faces so Unity can tint them with GUI.backgroundColor.
# ---------------------------------------------------------------------------

def rounded(draw, box, r, fill=None, outline=None, width=0):
    draw.rounded_rectangle(box, radius=r, fill=fill, outline=outline, width=width)


def button_skin(name, state):
    """96x96 button: dark outline, light face, darker 'lip' under it. state: normal/hover/down."""
    W = H = 96 * SS
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    o = 3 * SS          # outline thickness
    r = 22 * SS
    lip = 0 if state == "down" else 7 * SS
    top = 4 * SS if state == "down" else 0

    # drop shadow
    shadow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    sd = ImageDraw.Draw(shadow)
    rounded(sd, (2 * SS, 6 * SS, W - 2 * SS, H - 1), r, fill=(0, 0, 0, 120))
    shadow = shadow.filter(ImageFilter.GaussianBlur(3 * SS))
    img = Image.alpha_composite(img, shadow)
    d = ImageDraw.Draw(img)

    body = (o, top + o, W - o, H - 6 * SS)
    rounded(d, (body[0] - o, body[1] - o, body[2] + o, body[3] + o), r + o, fill=(10, 10, 14, 255))   # outline
    rounded(d, body, r, fill=(150, 150, 150, 255))                                                     # lip (darker)
    face_bottom = body[3] - lip
    face_col = {"normal": 235, "hover": 255, "down": 215}[state]
    rounded(d, (body[0], body[1], body[2], face_bottom), r, fill=(face_col,) * 3 + (255,))
    # glossy top highlight
    hl = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    hd = ImageDraw.Draw(hl)
    rounded(hd, (body[0] + 8 * SS, body[1] + 5 * SS, body[2] - 8 * SS, body[1] + 22 * SS), 10 * SS, fill=(255, 255, 255, 110))
    img = Image.alpha_composite(img, hl)
    save(down(img, (96, 96)), name)


def panel_skin(name, fill, border, radius=26, border_w=4, inner_line=True):
    W = H = 96 * SS
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    shadow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    sd = ImageDraw.Draw(shadow)
    rounded(sd, (4 * SS, 8 * SS, W - 4 * SS, H - 2 * SS), radius * SS, fill=(0, 0, 0, 140))
    img = Image.alpha_composite(img, shadow.filter(ImageFilter.GaussianBlur(4 * SS)))
    d = ImageDraw.Draw(img)
    m = 4 * SS
    rounded(d, (m, m, W - m, H - 8 * SS), radius * SS, fill=border)
    b = border_w * SS
    rounded(d, (m + b, m + b, W - m - b, H - 8 * SS - b), (radius - border_w) * SS, fill=fill)
    if inner_line:
        rounded(d, (m + b + 3 * SS, m + b + 3 * SS, W - m - b - 3 * SS, H - 8 * SS - b - 3 * SS),
                (radius - border_w - 3) * SS, outline=(255, 255, 255, 22), width=2 * SS)
    save(down(img, (96, 96)), name)


def dashed_card(name):
    W = H = 96 * SS
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    m = 4 * SS
    rounded(d, (m, m, W - m, H - m), 20 * SS, fill=(255, 255, 255, 18))
    # dashed outline: draw solid outline then punch gaps
    line = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ld = ImageDraw.Draw(line)
    rounded(ld, (m, m, W - m, H - m), 20 * SS, outline=(255, 255, 255, 90), width=3 * SS)
    mask = Image.new("L", (W, H), 255)
    md = ImageDraw.Draw(mask)
    step = 14 * SS
    for i in range(-H, W + H, step):
        md.polygon([(i, 0), (i + step // 2, 0), (i + step // 2 - H, H), (i - H, H)], fill=0)
    line.putalpha(ImageChops.multiply(line.getchannel("A"), mask))
    img = Image.alpha_composite(img, line)
    save(down(img, (96, 96)), name)


def field_skin(name, focused):
    W = H = 96 * SS
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    m = 3 * SS
    rounded(d, (m, m, W - m, H - m), 16 * SS, fill=YELLOW if focused else (10, 10, 14, 255))
    b = 3 * SS
    rounded(d, (m + b, m + b, W - m - b, H - m - b), 13 * SS, fill=(255, 250, 238, 255))
    # inner top shadow
    sh = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    shd = ImageDraw.Draw(sh)
    rounded(shd, (m + b, m + b, W - m - b, m + b + 14 * SS), 13 * SS, fill=(0, 0, 0, 40))
    img = Image.alpha_composite(img, sh.filter(ImageFilter.GaussianBlur(2 * SS)))
    save(down(img, (96, 96)), name)


def pill(name):
    """32x32, fully round ends at 16px radius. Use a 15px 9-slice border."""
    W = H = 32 * SS
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 0, W - 1, H - 1), radius=H // 2, fill=(10, 10, 14, 255))
    d.rounded_rectangle((2 * SS, 2 * SS, W - 1 - 2 * SS, H - 1 - 2 * SS), radius=H // 2 - 2 * SS, fill=(255, 255, 255, 255))
    save(down(img, (32, 32)), name)


def switch_skin(name, on):
    W, H = 128 * SS, 64 * SS
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 0, W - 1, H - 1), radius=H // 2, fill=(10, 10, 14, 255))
    d.rounded_rectangle((4 * SS, 4 * SS, W - 4 * SS, H - 4 * SS), radius=H // 2 - 4 * SS,
                        fill=(46, 204, 113, 255) if on else (70, 66, 86, 255))
    k = H - 14 * SS
    x = W - k - 7 * SS if on else 7 * SS
    d.ellipse((x, 7 * SS, x + k, 7 * SS + k), fill=(10, 10, 14, 255))
    d.ellipse((x + 3 * SS, 10 * SS, x + k - 3 * SS, 7 * SS + k - 3 * SS), fill=CREAM)
    save(down(img, (128, 64)), name)


# ---------------------------------------------------------------------------
# Icons
# ---------------------------------------------------------------------------

def avatar(name):
    """Crash-test dummy head, light gray so Unity can tint it with the player's color."""
    S = 128 * SS
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    c = S // 2
    R = int(S * 0.44)
    d.ellipse((c - R - 4 * SS, c - R - 4 * SS, c + R + 4 * SS, c + R + 4 * SS), fill=(10, 10, 14, 255))
    d.ellipse((c - R, c - R, c + R, c + R), fill=(235, 235, 235, 255))
    # shading
    shade = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    sd = ImageDraw.Draw(shade)
    sd.ellipse((c - R, c - R + int(R * 0.35), c + R, c + R + int(R * 0.6)), fill=(0, 0, 0, 55))
    mask = Image.new("L", (S, S), 0)
    ImageDraw.Draw(mask).ellipse((c - R, c - R, c + R, c + R), fill=255)
    shade.putalpha(ImageChops.multiply(shade.getchannel("A"), mask))
    img = Image.alpha_composite(img, shade)
    d = ImageDraw.Draw(img)
    # visor
    vw, vh = int(R * 1.25), int(R * 0.36)
    vy = c - int(R * 0.38)
    d.rounded_rectangle((c - vw // 2, vy, c + vw // 2, vy + vh), radius=vh // 2, fill=(16, 16, 22, 255))
    d.rounded_rectangle((c - vw // 2 + 10 * SS, vy + 6 * SS, c - 6 * SS, vy + 13 * SS), radius=3 * SS, fill=(255, 255, 255, 110))
    # crash-test target decal (yellow/black quadrants) on the cheek
    tr = int(R * 0.27)
    tx, ty = c, c + int(R * 0.40)
    d.ellipse((tx - tr - 2 * SS, ty - tr - 2 * SS, tx + tr + 2 * SS, ty + tr + 2 * SS), fill=(10, 10, 14, 255))
    for i, col in enumerate([(255, 201, 40, 255), (16, 16, 22, 255)] * 2):
        d.pieslice((tx - tr, ty - tr, tx + tr, ty + tr), i * 90, i * 90 + 90, fill=col)
    save(down(img, (128, 128)), name)


def bot_icon(name):
    S = 128 * SS
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    c = S // 2
    d.line((c, 14 * SS, c, 34 * SS), fill=(10, 10, 14, 255), width=10 * SS)
    d.ellipse((c - 11 * SS, 6 * SS, c + 11 * SS, 28 * SS), fill=(10, 10, 14, 255))
    d.ellipse((c - 7 * SS, 10 * SS, c + 7 * SS, 24 * SS), fill=(255, 107, 26, 255))
    d.rounded_rectangle((16 * SS, 32 * SS, S - 16 * SS, S - 14 * SS), radius=24 * SS, fill=(10, 10, 14, 255))
    d.rounded_rectangle((22 * SS, 38 * SS, S - 22 * SS, S - 20 * SS), radius=19 * SS, fill=(200, 200, 205, 255))
    for ex in (c - 22 * SS, c + 22 * SS):
        d.ellipse((ex - 11 * SS, 58 * SS, ex + 11 * SS, 80 * SS), fill=(16, 16, 22, 255))
        d.ellipse((ex - 4 * SS, 63 * SS, ex + 3 * SS, 70 * SS), fill=(255, 255, 255, 255))
    d.rounded_rectangle((c - 22 * SS, 90 * SS, c + 22 * SS, 98 * SS), radius=4 * SS, fill=(16, 16, 22, 255))
    save(down(img, (128, 128)), name)


def mic_icon(name):
    S = 64 * SS
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    c = S // 2
    d.rounded_rectangle((c - 9 * SS, 6 * SS, c + 9 * SS, 38 * SS), radius=9 * SS, fill=(255, 255, 255, 255))
    d.arc((c - 17 * SS, 14 * SS, c + 17 * SS, 46 * SS), 0, 180, fill=(255, 255, 255, 255), width=5 * SS)
    d.line((c, 46 * SS, c, 56 * SS), fill=(255, 255, 255, 255), width=5 * SS)
    d.line((c - 11 * SS, 57 * SS, c + 11 * SS, 57 * SS), fill=(255, 255, 255, 255), width=5 * SS)
    save(down(img, (64, 64)), name)


def crown_icon(name):
    S = 64 * SS
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    pts = [(8, 50), (6, 18), (20, 32), (32, 10), (44, 32), (58, 18), (56, 50)]
    pts = [(x * SS, y * SS) for x, y in pts]
    d.polygon(pts, fill=(255, 255, 255, 255))
    d.rounded_rectangle((8 * SS, 46 * SS, 56 * SS, 56 * SS), radius=3 * SS, fill=(255, 255, 255, 255))
    save(down(img, (64, 64)), name)


def dot(name):
    S = 32 * SS
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(img).ellipse((0, 0, S - 1, S - 1), fill=(255, 255, 255, 255))
    save(down(img, (32, 32)), name)


# ---------------------------------------------------------------------------
# Background pieces
# ---------------------------------------------------------------------------

def stripes(name):
    """Tileable 45-degree hazard stripes, 64x64."""
    S = 64 * SS
    img = Image.new("RGBA", (S, S), INK)
    d = ImageDraw.Draw(img)
    w = S // 2
    for i in range(-2, 3):
        x = i * S
        d.polygon([(x, S), (x + w, S), (x + w + S, 0), (x + S, 0)], fill=YELLOW)
    save(down(img, (64, 64)), name)


def vignette(name):
    W, H = 512, 288
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = img.load()
    for y in range(H):
        for x in range(W):
            dx = (x - W / 2) / (W / 2)
            dy = (y - H / 2) / (H / 2)
            r = math.sqrt(dx * dx * 0.8 + dy * dy)
            a = max(0.0, min(1.0, (r - 0.35) / 0.9))
            px[x, y] = (12, 8, 24, int(40 + 190 * a * a))
    save(img, name)


def spotlight(name):
    W, H = 256, 512
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = img.load()
    for y in range(H):
        t = y / H
        half = 30 + t * 110
        for x in range(W):
            dx = abs(x - W / 2)
            if dx < half:
                edge = 1 - dx / half
                a = edge ** 1.6 * (1 - t) ** 0.6 * 0.55
                px[x, y] = (255, 236, 180, int(255 * a))
    save(img.filter(ImageFilter.GaussianBlur(6)), name)


# ---------------------------------------------------------------------------
# Logo
# ---------------------------------------------------------------------------

def outlined_text(canvas, xy, text, fnt, fill, outline, ow, anchor="mm", shadow_off=0):
    d = ImageDraw.Draw(canvas)
    if shadow_off:
        d.text((xy[0] + shadow_off, xy[1] + shadow_off), text, font=fnt, fill=(0, 0, 0, 200), anchor=anchor,
               stroke_width=ow, stroke_fill=(0, 0, 0, 200))
    d.text(xy, text, font=fnt, fill=fill, anchor=anchor, stroke_width=ow, stroke_fill=outline)


def gradient_text(size, xy, text, fnt, top, bottom, anchor="mm"):
    mask = Image.new("L", size, 0)
    ImageDraw.Draw(mask).text(xy, text, font=fnt, fill=255, anchor=anchor)
    grad = Image.new("RGBA", size)
    gd = ImageDraw.Draw(grad)
    bbox = mask.getbbox() or (0, 0, size[0], size[1])
    for y in range(size[1]):
        t = min(1, max(0, (y - bbox[1]) / max(1, bbox[3] - bbox[1])))
        col = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,)
        gd.line((0, y, size[0], y), fill=col)
    out = Image.new("RGBA", size, (0, 0, 0, 0))
    out.paste(grad, (0, 0), mask)
    return out


def logo(name):
    W, H = 1500 * 2, 640 * 2   # 2x supersample is plenty for this size
    k = 2
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    # starburst behind the words
    burst = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    bd = ImageDraw.Draw(burst)
    cx, cy = W // 2, int(H * 0.47)
    rays = 22
    R = int(W * 0.40)
    for i in range(rays):
        a0 = (i / rays) * 2 * math.pi
        a1 = a0 + math.pi / rays
        col = (255, 107, 26, 255) if i % 2 == 0 else (230, 57, 70, 255)
        bd.polygon([(cx, cy), (cx + R * math.cos(a0), cy + R * 0.55 * math.sin(a0)),
                    (cx + R * math.cos(a1), cy + R * 0.55 * math.sin(a1))], fill=col)
    emask = Image.new("L", (W, H), 0)
    ImageDraw.Draw(emask).ellipse((cx - R, cy - int(R * 0.55), cx + R, cy + int(R * 0.55)), fill=255)
    emask = emask.filter(ImageFilter.GaussianBlur(40 * k))
    burst.putalpha(ImageChops.multiply(burst.getchannel("A"), emask))
    img = Image.alpha_composite(img, burst)

    big = font("LuckiestGuy-Regular.ttf", 230 * k)
    huge = font("LuckiestGuy-Regular.ttf", 300 * k)

    words = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    l1 = (W // 2, int(H * 0.30))
    l2 = (W // 2, int(H * 0.655))
    # thick outer outline + shadow, then gradient fill on top
    outlined_text(words, (l1[0], l1[1] + 14 * k), "KNOCKBACK", big, (0, 0, 0, 0), (0, 0, 0, 160), 26 * k)
    outlined_text(words, l1, "KNOCKBACK", big, INK, INK, 22 * k)
    outlined_text(words, (l2[0], l2[1] + 16 * k), "RUMBLE", huge, (0, 0, 0, 0), (0, 0, 0, 160), 28 * k)
    outlined_text(words, l2, "RUMBLE", huge, INK, INK, 24 * k)
    words = Image.alpha_composite(words, gradient_text((W, H), l1, "KNOCKBACK", big, (255, 250, 230), (255, 214, 90)))
    words = Image.alpha_composite(words, gradient_text((W, H), l2, "RUMBLE", huge, (255, 224, 70), (255, 120, 20)))
    words = words.rotate(4, resample=Image.BICUBIC, center=(W // 2, H // 2))
    img = Image.alpha_composite(img, words)

    # "LIVE FROM THE JUNKYARD" banner
    tag = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    td = ImageDraw.Draw(tag)
    tf = font("LilitaOne-Regular.ttf", 58 * k)
    text = "LIVE FROM THE JUNKYARD"
    tw = td.textlength(text, font=tf)
    bx0, by0 = W // 2 - tw / 2 - 50 * k, int(H * 0.86) - 46 * k
    bx1, by1 = W // 2 + tw / 2 + 50 * k, int(H * 0.86) + 46 * k
    td.rounded_rectangle((bx0 - 7 * k, by0 - 7 * k, bx1 + 7 * k, by1 + 7 * k), radius=20 * k, fill=INK)
    td.rounded_rectangle((bx0, by0, bx1, by1), radius=14 * k, fill=(230, 57, 70, 255))
    td.ellipse((bx0 + 22 * k, int(H * 0.86) - 12 * k, bx0 + 46 * k, int(H * 0.86) + 12 * k), fill=CREAM)
    td.text((W // 2 + 14 * k, int(H * 0.86)), text, font=tf, fill=CREAM, anchor="mm")
    tag = tag.rotate(4, resample=Image.BICUBIC, center=(W // 2, H // 2))
    img = Image.alpha_composite(img, tag)

    img = img.crop(img.getbbox())
    target_w = 1400
    img = img.resize((target_w, int(img.height * target_w / img.width)), Image.LANCZOS)
    save(img, name)


def title_banner(name, text):
    """A small headline like 'THE GREEN ROOM' in the logo style."""
    k = 2
    W, H = 1400 * k, 260 * k
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    f = font("LuckiestGuy-Regular.ttf", 150 * k)
    c = (W // 2, H // 2)
    outlined_text(img, (c[0], c[1] + 10 * k), text, f, (0, 0, 0, 0), (0, 0, 0, 160), 18 * k)
    outlined_text(img, c, text, f, INK, INK, 15 * k)
    img = Image.alpha_composite(img, gradient_text((W, H), c, text, f, (255, 240, 120), (255, 150, 30)))
    img = img.rotate(2.5, resample=Image.BICUBIC, center=c)
    img = img.crop(img.getbbox())
    img = img.resize((img.width // k, img.height // k), Image.LANCZOS)
    save(img, name)


if __name__ == "__main__":
    for st in ("normal", "hover", "down"):
        button_skin(f"btn_{st}.png", st)
    panel_skin("panel.png", (32, 28, 48, 238), (10, 10, 14, 255))
    panel_skin("card.png", (52, 47, 74, 255), (10, 10, 14, 255), radius=22, border_w=3)
    dashed_card("card_empty.png")
    field_skin("field.png", False)
    field_skin("field_focus.png", True)
    pill("pill.png")
    switch_skin("switch_on.png", True)
    switch_skin("switch_off.png", False)
    avatar("avatar.png")
    bot_icon("bot.png")
    mic_icon("mic.png")
    crown_icon("crown.png")
    dot("dot.png")
    stripes("stripes.png")
    vignette("vignette.png")
    spotlight("spotlight.png")
    logo("logo.png")
    title_banner("title_lobby.png", "THE GREEN ROOM")
    title_banner("title_paused.png", "COMMERCIAL BREAK")
