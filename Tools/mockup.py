"""Renders mockups of the new menus using the same art and the same layout numbers as MenuUI.cs."""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageChops

ART = Path(sys.argv[1]); FONTS = Path(sys.argv[2]); OUT = Path(sys.argv[3])
W, H = 1920, 1080
cx = W / 2

YELLOW = (1.0, 0.79, 0.16); TEAL = (0.18, 0.77, 0.71); DARK = (0.36, 0.33, 0.50); RED = (0.90, 0.27, 0.30)
GREEN = (0.18, 0.80, 0.44); INK = (20, 18, 28); CREAM = (255, 244, 214); MUTED = (190, 182, 210)
HUMAN = [(1, .5, .1), (.15, .8, .95), (.55, .95, .2), (.95, .25, .85), (1, .92, .2), (.95, .95, .95), (.1, .65, .55), (.9, .15, .2)]

_cache = {}
def tex(n):
    if n not in _cache: _cache[n] = Image.open(ART / n).convert("RGBA")
    return _cache[n]
def F(name, size): return ImageFont.truetype(str(FONTS / name), size)
LIL = "LilitaOne-Regular.ttf"; NB = "Nunito-Black.ttf"; NBo = "Nunito-Bold.ttf"

def tint(img, t, a=1.0):
    if t is None and a == 1.0: return img
    t = t or (1, 1, 1)
    r, g, b, al = img.split()
    r = r.point(lambda v: int(v * t[0])); g = g.point(lambda v: int(v * t[1])); b = b.point(lambda v: int(v * t[2]))
    al = al.point(lambda v: int(v * a))
    return Image.merge("RGBA", (r, g, b, al))

def nine(canvas, name, rect, border, t=None, a=1.0):
    src = tint(tex(name), t, a); sw, sh = src.size
    x, y, w, h = [int(round(v)) for v in rect]; b = border
    xs = [(0, b, x, b), (b, sw - b, x + b, w - 2 * b), (sw - b, sw, x + w - b, b)]
    ys = [(0, b, y, b), (b, sh - b, y + b, h - 2 * b), (sh - b, sh, y + h - b, b)]
    for sx0, sx1, dx, dw in xs:
        for sy0, sy1, dy, dh in ys:
            if dw <= 0 or dh <= 0: continue
            piece = src.crop((sx0, sy0, sx1, sy1)).resize((dw, dh), Image.BILINEAR)
            canvas.alpha_composite(piece, (dx, dy))

def draw(canvas, name, rect, t=None, a=1.0):
    x, y, w, h = [int(round(v)) for v in rect]
    canvas.alpha_composite(tint(tex(name), t, a).resize((w, h), Image.LANCZOS), (x, y))

def text(canvas, rect, s, font, size, color, align="left", shadow=True, alpha=255):
    d = ImageDraw.Draw(canvas); f = F(font, size)
    x, y, w, h = rect
    tw = d.textlength(s, font=f)
    if align == "center": tx = x + (w - tw) / 2
    elif align == "right": tx = x + w - tw
    else: tx = x
    ty = y + h / 2
    if shadow: d.text((tx + 2, ty + 3), s, font=f, fill=(0, 0, 0, int(170 * alpha / 255)), anchor="lm")
    d.text((tx, ty), s, font=f, fill=color[:3] + (alpha,), anchor="lm")

def button(c, rect, label, t, sub=None, size=34, hover=False, dark_text=None):
    nine(c, "btn_hover.png" if hover else "btn_normal.png", rect, 24, t)
    x, y, w, h = rect
    ink = dark_text if dark_text is not None else (t in (YELLOW, TEAL, GREEN))
    col = INK if ink else CREAM
    if sub:
        text(c, (x, y + 8, w, h * 0.5), label, LIL, size, col, "center", shadow=not ink)
        text(c, (x, y + h * 0.5, w, h * 0.32), sub, NBo, 20, col, "center", shadow=False, alpha=200)
    else:
        text(c, (x, y - 2, w, h - 6), label, LIL, size, col, "center", shadow=not ink)

def pill(c, rect, t, a=1.0): nine(c, "pill.png", rect, 15, t, a)

def background(c):
    # stand-in for the live 3D course behind the menu
    g = Image.new("RGBA", (W, H)); gd = ImageDraw.Draw(g)
    for yy in range(H):
        k = yy / H
        gd.line((0, yy, W, yy), fill=(int(110 - 60 * k), int(150 - 80 * k), int(190 - 90 * k), 255))
    gd.polygon([(300, 760), (1620, 760), (1900, 1080), (20, 1080)], fill=(95, 98, 105, 255))
    for i in range(14):
        gd.rectangle((80 + i * 130, 560 - (i * 37 % 120), 160 + i * 130, 760), fill=(110 + i * 3, 70, 45, 255))
    c.alpha_composite(g)
    ov = Image.new("RGBA", (W, H), (12, 8, 24, int(255 * 0.35))); c.alpha_composite(ov)
    draw(c, "vignette.png", (0, 0, W, H))
    draw(c, "spotlight.png", (cx - 760, -40, 300, 640), a=0.8)
    draw(c, "spotlight.png", (cx + 460, -40, 300, 640), a=0.8)
    st = tex("stripes.png")
    for x in range(0, W, 64):
        c.alpha_composite(st.crop((0, 0, 64, 18)), (x, 0)); c.alpha_composite(st.crop((0, 0, 64, 18)), (x, H - 18))

def main_menu(steam_ok=True, tools=False):
    c = Image.new("RGBA", (W, H)); background(c)
    lw = 860; lh = lw * tex("logo.png").height / tex("logo.png").width
    draw(c, "logo.png", (cx - lw / 2, 14, lw, lh))
    px, py, pw, ph = cx - 340, 470, 680, 500
    nine(c, "panel.png", (px, py, pw, ph), 30)
    text(c, (px + 40, py + 26, 300, 30), "YOUR NAME", LIL, 24, (127, 219, 218))
    nine(c, "field.png", (px + 40, py + 60, 600, 66), 20)
    text(c, (px + 62, py + 60, 560, 66), "Michael", NB, 30, INK, shadow=False)
    button(c, (px + 40, py + 148, 600, 104), "HOST A GAME", YELLOW, "Invite your Steam friends", 44, hover=True)
    button(c, (px + 40, py + 266, 600, 88), "PLAY OFFLINE", DARK, "You vs 7 bots", 36)
    sr = (px + 40, py + 370, 600, 42)
    pill(c, sr, (0.16, 0.15, 0.22))
    dot = GREEN if steam_ok else RED
    draw(c, "dot.png", (sr[0] + 16, sr[1] + 13, 16, 16), dot)
    text(c, (sr[0] + 44, sr[1], 540, 42), "Steam: logged in as Michael" if steam_ok else "Steam isn't running - online play is off", NBo, 20, CREAM, shadow=False)
    text(c, (px + 40, py + 418, 600, 30), "Joining a friend? Accept their Steam invite, or right-click", NBo, 19, MUTED, "center", shadow=False)
    text(c, (px + 40, py + 444, 600, 30), "them in your Steam friends list and pick Join Game.", NBo, 19, MUTED, "center", shadow=False)
    button(c, (40, H - 86, 280, 60), "TESTING TOOLS", DARK, size=26)
    button(c, (W - 200, H - 86, 160, 60), "QUIT", RED, size=26)
    if tools:
        tx, ty, tw, th = 40, H - 86 - 250, 560, 236
        nine(c, "panel.png", (tx, ty, tw, th), 30)
        text(c, (tx + 30, ty + 20, 500, 30), "DIRECT CONNECT (testing without Steam)", LIL, 22, (127, 219, 218))
        text(c, (tx + 30, ty + 64, 60, 52), "IP", NB, 22, CREAM)
        nine(c, "field.png", (tx + 80, ty + 62, 260, 54), 20); text(c, (tx + 98, ty + 62, 230, 54), "127.0.0.1", NB, 24, INK, shadow=False)
        text(c, (tx + 356, ty + 64, 70, 52), "Port", NB, 22, CREAM)
        nine(c, "field.png", (tx + 420, ty + 62, 110, 54), 20); text(c, (tx + 436, ty + 62, 90, 54), "7777", NB, 24, INK, shadow=False)
        button(c, (tx + 30, ty + 140, 240, 70), "HOST DIRECT", TEAL, size=26)
        button(c, (tx + 290, ty + 140, 240, 70), "JOIN DIRECT", TEAL, size=26)
    return c

def lobby(host=True, n=3, talking=1, bots=True):
    c = Image.new("RGBA", (W, H)); background(c)
    tw_ = 640; th_ = tw_ * tex("title_lobby.png").height / tex("title_lobby.png").width
    draw(c, "title_lobby.png", (cx - tw_ / 2, 30, tw_, th_))
    sp = (cx - 230, 148, 460, 42); pill(c, sp, (0.16, 0.15, 0.22))
    text(c, sp, "STEAM LOBBY  -  FRIENDS ONLY", LIL, 22, CREAM, "center", shadow=False)

    lx, ly, lw, lh = cx - 740, 214, 940, 640
    nine(c, "panel.png", (lx, ly, lw, lh), 30)
    text(c, (lx + 40, ly + 26, 400, 44), "CONTESTANTS", LIL, 38, CREAM)
    text(c, (lx + lw - 240, ly + 26, 200, 44), f"{n} / 8", LIL, 38, (255, 201, 40), "right")
    names = ["Michael", "BigTony420", "xX_Dale_Xx", "Sarah", "Keon", "Lil Ricky", "Dre", "Mo"]
    cw, ch, gx, gy = 418, 116, 24, 16
    for i in range(8):
        col, row = i % 2, i // 2
        x = lx + 40 + col * (cw + gx); y = ly + 96 + row * (ch + gy)
        if i < n:
            nine(c, "card.png", (x, y, cw, ch), 26)
            colr = HUMAN[i]
            draw(c, "avatar.png", (x + 16, y + 16, 84, 84), colr)
            text(c, (x + 116, y + 16, cw - 130, 44), names[i], LIL, 32, CREAM)
            bx = x + 116; by = y + 66
            if i == 0:
                pill(c, (bx, by, 104, 34), YELLOW); draw(c, "crown.png", (bx + 10, by + 6, 22, 22), (0.08, 0.07, 0.11))
                text(c, (bx + 36, by, 60, 34), "HOST", LIL, 20, INK, shadow=False); bx += 114
                pill(c, (bx, by, 70, 34), TEAL); text(c, (bx, by, 70, 34), "YOU", LIL, 20, INK, "center", shadow=False); bx += 80
            if i == talking:
                pill(c, (bx, by, 138, 34), GREEN); draw(c, "mic.png", (bx + 10, by + 6, 22, 22), (0.08, 0.07, 0.11))
                text(c, (bx + 36, by, 96, 34), "TALKING", LIL, 20, INK, shadow=False)
        else:
            nine(c, "card_empty.png", (x, y, cw, ch), 26)
            if bots:
                draw(c, "bot.png", (x + 22, y + 22, 72, 72), a=0.75)
                text(c, (x + 116, y + 20, cw - 130, 40), "BOT", LIL, 30, CREAM, alpha=200)
                text(c, (x + 116, y + 62, cw - 130, 30), "fills this spot", NBo, 20, MUTED, shadow=False)
            else:
                text(c, (x, y + 22, cw, 40), "OPEN SPOT", LIL, 30, CREAM, "center", alpha=170)
                text(c, (x, y + 62, cw, 30), "Invite a friend", NBo, 20, MUTED, "center", shadow=False)

    rx, ry, rw, rh = cx + 228, 214, 512, 640
    nine(c, "panel.png", (rx, ry, rw, rh), 30)
    text(c, (rx + 40, ry + 26, 400, 44), "THE SHOW", LIL, 38, CREAM)
    text(c, (rx + 40, ry + 82, 440, 32), "Round 1:  Scrap Sprint (race)", NB, 23, CREAM, shadow=False)
    text(c, (rx + 40, ry + 116, 440, 32), "8 dummies  -  top 5 qualify", NB, 23, MUTED, shadow=False)
    if host:
        nine(c, "card.png", (rx + 32, ry + 168, rw - 64, 84), 26)
        text(c, (rx + 56, ry + 168, 300, 84), "Fill empty spots with bots", NB, 22, CREAM, shadow=False)
        draw(c, "switch_on.png" if bots else "switch_off.png", (rx + rw - 32 - 24 - 100, ry + 186, 100, 50))
        button(c, (rx + 32, ry + 280, rw - 64, 88), "INVITE FRIENDS", TEAL, "Opens your Steam friends list", 32)
        button(c, (rx + 32, ry + 392, rw - 64, 120), "START THE SHOW", YELLOW, "Everyone in the lobby races", 46, hover=True)
        button(c, (rx + 32, ry + 540, rw - 64, 72), "LEAVE LOBBY", RED, size=28)
    else:
        nine(c, "card.png", (rx + 32, ry + 168, rw - 64, 340), 26)
        text(c, (rx + 32, ry + 270, rw - 64, 50), "WAITING FOR THE HOST", LIL, 32, (255, 201, 40), "center")
        text(c, (rx + 32, ry + 322, rw - 64, 36), "to start the show...", NB, 24, CREAM, "center", shadow=False)
        button(c, (rx + 32, ry + 540, rw - 64, 72), "LEAVE LOBBY", RED, size=28)

    mb = (cx - 400, 884, 800, 56); pill(c, mb, (0.16, 0.15, 0.22))
    pill(c, (mb[0] + 8, mb[1] + 8, 132, 40), GREEN); draw(c, "mic.png", (mb[0] + 20, mb[1] + 14, 28, 28), (0.08, 0.07, 0.11))
    text(c, (mb[0] + 54, mb[1] + 8, 80, 40), "MIC ON", LIL, 22, INK, shadow=False)
    text(c, (mb[0] + 156, mb[1], 630, 56), "Everyone can hear you.  Press M to mute.", NBo, 22, CREAM, shadow=False)
    return c

def pause(online=True, host=True):
    c = Image.new("RGBA", (W, H)); background(c)
    c.alpha_composite(Image.new("RGBA", (W, H), (8, 6, 16, int(255 * 0.45))))
    tw_ = 760; th_ = tw_ * tex("title_paused.png").height / tex("title_paused.png").width
    ty = H * 0.14
    draw(c, "title_paused.png", (cx - tw_ / 2, ty, tw_, th_))
    px, py, pw = cx - 300, ty + th_ + 24, 600
    ph = 84 + 108 + (100 if online and host else 0) + 92 + 56
    nine(c, "panel.png", (px, py, pw, ph), 30)
    text(c, (px, py + 26, pw, 34), "The race keeps going while you're in here!" if online else "Take a breather.", NB, 23, CREAM, "center", shadow=False)
    y = py + 84
    button(c, (px + 40, y, pw - 80, 92), "RESUME", YELLOW, size=40, hover=True); y += 108
    if online and host:
        button(c, (px + 40, y, pw - 80, 88), "BACK TO LOBBY", TEAL, "Everyone returns to the green room", 30); y += 100
    button(c, (px + 40, y, pw - 80, 76), "END GAME FOR EVERYONE" if online and host else ("LEAVE GAME" if online else "QUIT TO MENU"), RED, size=30); y += 92
    text(c, (px, y, pw, 30), "Mic ON  -  press M to mute", NBo, 20, MUTED, "center", shadow=False)
    return c

for name, img in [("mock_main.png", main_menu()), ("mock_main_tools.png", main_menu(False, True)),
                  ("mock_lobby_host.png", lobby()), ("mock_lobby_client.png", lobby(False, 5, 3, False)),
                  ("mock_pause.png", pause())]:
    img.convert("RGB").save(OUT / name); print("wrote", name)
