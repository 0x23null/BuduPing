#!/usr/bin/env python3
"""
make_transparent.py — BuduPing sticker asset pipeline.

Turns a sticker GIF with a baked-in solid background (white, black, teal, ...)
into a truly transparent GIF:

  1. Extracts every frame as RGBA.
  2. Detects the background colour as the most common colour along the
     4 image borders.
  3. Flood-fills from every border pixel, removing only pixels close to the
     background colour AND connected to the border. Yier's white body is
     safe because the brown outline breaks the connection.
  4. Resizes so the longest side is within [min_side, max_side].
  5. Decimates frames (keeping total duration) so the file stays small.
  6. Re-encodes as GIF with 1-bit transparency (palette index 0).

Usage:
    python3 make_transparent.py INPUT.gif OUTPUT.gif [--max-side 320]
        [--min-side 240] [--max-frames 24] [--max-kb 600] [--tolerance 48]

Exit code 0 = success, and prints a JSON summary on stdout.
"""

import argparse
import json
import os
import sys
from collections import Counter, deque

from PIL import Image, ImageSequence


def detect_bg_color(rgba):
    """Most common RGB colour among the 4 border pixel rows/cols."""
    w, h = rgba.size
    px = rgba.load()
    edge = []
    step = max(1, w // 200)
    for x in range(0, w, step):
        edge.append(px[x, 0][:3])
        edge.append(px[x, h - 1][:3])
    step = max(1, h // 200)
    for y in range(0, h, step):
        edge.append(px[0, y][:3])
        edge.append(px[w - 1, y][:3])
    return Counter(edge).most_common(1)[0][0]


def flood_fill_transparent(rgba, bg, tolerance=40, seed_tolerance=48,
                           corner_radius=16):
    """
    Set alpha=0 for background pixels connected to the image CORNERS.

    Adaptive two-level flood fill (handles slightly graded backgrounds):
      * the effective colour tolerance is derived from the measured spread
        of background-like border colours: tol = clamp(spread + 8, 16, 40).
        A flat background gets a tight tolerance (white character bodies
        stay safe even where anti-aliased outlines leak); a graded one gets
        a wider tolerance so the whole gradient is peeled.
      * seeds = pixels within `corner_radius` of the 4 corners whose colour
        is within `seed_tolerance` of the modal border colour `bg`. Seeding
        from corners only (not the whole border) protects characters that
        are cropped by a frame edge.
      * each seed flood-fills using ITS OWN colour as reference, so a smooth
        gradient background is removed while the character's dark outline
        (far from every seed colour) stops the fill.
    Returns the number of pixels cleared.
    """
    w, h = rgba.size
    px = rgba.load()
    br, bgc, bb = bg

    def near(c, ref, tol):
        return (abs(c[0] - ref[0]) < tol and abs(c[1] - ref[1]) < tol
                and abs(c[2] - ref[2]) < tol)

    # Sample border colours to measure background spread.
    border_cols = []
    for x in range(0, w, 3):
        border_cols.append(px[x, 0][:3])
        border_cols.append(px[x, h - 1][:3])
    for y in range(0, h, 3):
        border_cols.append(px[0, y][:3])
        border_cols.append(px[w - 1, y][:3])
    bg_like = [c for c in border_cols if near(c, (br, bgc, bb), seed_tolerance)]
    if bg_like:
        spread = max(max(c[i] for c in bg_like) - min(c[i] for c in bg_like)
                     for i in range(3))
    else:
        spread = 0
    tol = min(tolerance, max(16, spread + 8))

    corners = [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)]
    seeds = []
    for cx, cy in corners:
        x0, x1 = max(0, cx - corner_radius), min(w, cx + corner_radius + 1)
        y0, y1 = max(0, cy - corner_radius), min(h, cy + corner_radius + 1)
        for x in range(x0, x1):
            for y in range(y0, y1):
                r, g, b, a = px[x, y]
                if a != 0 and near((r, g, b), (br, bgc, bb), seed_tolerance):
                    seeds.append((x, y))

    cleared = 0
    for sx, sy in seeds:
        sr, sg, sb, sa = px[sx, sy]
        if sa == 0:  # already cleared by an earlier seed
            continue
        # BFS from this seed with the seed's own colour as reference.
        # A pixel is cleared only if it is close to the seed colour AND
        # still within the loose background band — this stops the fill
        # from walking along a dark character outline into the drawing.
        ref = (sr, sg, sb)
        q = deque([(sx, sy)])
        seen = {(sx, sy)}
        while q:
            x, y = q.popleft()
            r, g, b, a = px[x, y]
            if a == 0 or not near((r, g, b), ref, tol):
                continue
            if not near((r, g, b), (br, bgc, bb), seed_tolerance):
                continue
            px[x, y] = (r, g, b, 0)
            cleared += 1
            for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if 0 <= nx < w and 0 <= ny < h and (nx, ny) not in seen:
                    seen.add((nx, ny))
                    nr, ng, nb, na = px[nx, ny]
                    if (na != 0 and near((nr, ng, nb), ref, tol)
                            and near((nr, ng, nb), (br, bgc, bb), seed_tolerance)):
                        q.append((nx, ny))

    # Corner cleanup: the contract requires alpha=0 at all 4 corners of
    # every frame. If a corner is still opaque (e.g. a soft glow/vignette
    # pixel the main pass rejected), peel its own connected region with a
    # tight tolerance around the corner pixel's own colour.
    for cx, cy in corners:
        cr, cg, cb, ca = px[cx, cy]
        if ca == 0:
            continue
        ref = (cr, cg, cb)
        q = deque([(cx, cy)])
        seen = {(cx, cy)}
        while q:
            x, y = q.popleft()
            r, g, b, a = px[x, y]
            if a == 0 or not near((r, g, b), ref, 12):
                continue
            px[x, y] = (r, g, b, 0)
            cleared += 1
            for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if 0 <= nx < w and 0 <= ny < h and (nx, ny) not in seen:
                    seen.add((nx, ny))
                    nr, ng, nb, na = px[nx, ny]
                    if na != 0 and near((nr, ng, nb), ref, 12):
                        q.append((nx, ny))
    return cleared


def rgba_to_gif_frame(rgba):
    """
    Convert RGBA (binary alpha) to P-mode for GIF saving, reserving
    palette index 255 for transparent pixels. Returns (P image, transp_index).
    """
    TRANSP = 255
    alpha = rgba.getchannel("A")
    # opaque_mask: 255 where the pixel is opaque
    opaque_mask = alpha.point(lambda v: 255 if v >= 128 else 0)
    rgb = rgba.convert("RGB")
    # Paint transparent pixels white so the adaptive palette is spent on
    # real colours instead of the leftover background RGB.
    rgb = Image.composite(rgb, Image.new("RGB", rgb.size, (255, 255, 255)),
                          opaque_mask)
    p = rgb.convert("P", palette=Image.ADAPTIVE, colors=255)
    # L mask: 255 where the pixel is transparent
    tmask = alpha.point(lambda v: 255 if v < 128 else 0)
    p.paste(TRANSP, tmask)
    return p, TRANSP


def decimate(frames, durations, max_frames):
    """Evenly sample frames, merging durations so total play time is kept."""
    n = len(frames)
    if n <= max_frames:
        return frames, durations
    step = n / max_frames
    out_f, out_d = [], []
    for i in range(max_frames):
        start = int(round(i * step))
        end = int(round((i + 1) * step))
        end = max(end, start + 1)
        out_f.append(frames[start])
        out_d.append(sum(durations[start:end]))
    return out_f, out_d


def extract_frames(src):
    """
    Extract fully composited RGBA frames from a GIF, honouring partial
    (delta) frames and disposal methods. Returns (frames, durations).
    """
    im = Image.open(src)
    if im.format != "GIF":
        raise ValueError(f"{src} is not a GIF (format={im.format})")
    canvas = Image.new("RGBA", im.size, (0, 0, 0, 0))
    frames, durations = [], []
    for i in range(im.n_frames):
        im.seek(i)
        tile = im.tile[0][1] if im.tile else (0, 0) + im.size
        x0, y0, x1, y1 = tile
        patch = im.convert("RGBA").crop((x0, y0, x1, y1))
        canvas.paste(patch, (x0, y0), patch)
        frames.append(canvas.copy())
        durations.append(im.info.get("duration", 100) or 100)
        disp = getattr(im, "disposal_method", None)
        if disp is None:
            disp = im.info.get("disposal", 0)
        if disp == 2:  # restore to background: clear this frame's region
            canvas.paste((0, 0, 0, 0), (x0, y0, x1, y1))
    return frames, durations


def process(src, dst, max_side=320, min_side=240, max_frames=24,
            max_kb=600, tolerance=48):
    frames, durations = extract_frames(src)
    n_in = len(frames)

    total_cleared = 0
    for rgba in frames:
        bg = detect_bg_color(rgba)
        total_cleared += flood_fill_transparent(rgba, bg, tolerance)

    # Resize: longest side into [min_side, max_side]; never upscale.
    w, h = frames[0].size
    longest = max(w, h)
    target = min(max_side, max(min_side, longest))
    if longest > target:
        scale = target / longest
        nw, nh = max(1, int(round(w * scale))), max(1, int(round(h * scale)))
        frames = [f.resize((nw, nh), Image.LANCZOS) for f in frames]
        w, h = nw, nh

    # Decimate frames until under size budget (binary search on frame count).
    frames, durations = decimate(frames, durations, max_frames)

    def encode(frames, durations, path):
        converted = [rgba_to_gif_frame(f) for f in frames]
        gif_frames = [c[0] for c in converted]
        transp = converted[0][1]
        gif_frames[0].save(path, save_all=True, append_images=gif_frames[1:],
                           duration=durations, loop=0, transparency=transp,
                           disposal=2, optimize=True)

    tmp = dst + ".tmp.gif"
    encode(frames, durations, tmp)
    size_kb = os.path.getsize(tmp) / 1024
    lo_frames = max_frames
    while size_kb > max_kb and lo_frames > 8:
        lo_frames = max(8, lo_frames // 2)
        frames, durations = decimate(frames, durations, lo_frames)
        encode(frames, durations, tmp)
        size_kb = os.path.getsize(tmp) / 1024
    os.replace(tmp, dst)

    # Verify: alpha at the 4 corners of EVERY frame must be 0.
    check = Image.open(dst)
    bad_frames = 0
    for f in ImageSequence.Iterator(check):
        rgba = f.convert("RGBA")
        cw, ch = rgba.size
        corners = [rgba.getpixel((x, y))[3]
                   for x, y in [(0, 0), (cw - 1, 0), (0, ch - 1), (cw - 1, ch - 1)]]
        if any(a != 0 for a in corners):
            bad_frames += 1

    return {
        "src": src,
        "dst": dst,
        "size": [w, h],
        "frames_in": n_in,
        "frames_out": len(frames),
        "duration_ms": sum(durations),
        "size_kb": round(size_kb, 1),
        "bg_pixels_cleared": total_cleared,
        "corners_ok": bad_frames == 0,
        "frames_with_bad_corners": bad_frames,
    }


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("input")
    ap.add_argument("output")
    ap.add_argument("--max-side", type=int, default=320)
    ap.add_argument("--min-side", type=int, default=240)
    ap.add_argument("--max-frames", type=int, default=24)
    ap.add_argument("--max-kb", type=int, default=600)
    ap.add_argument("--tolerance", type=int, default=48)
    args = ap.parse_args()
    try:
        summary = process(args.input, args.output, args.max_side, args.min_side,
                          args.max_frames, args.max_kb, args.tolerance)
    except Exception as e:  # noqa: BLE001 — report as JSON, nonzero exit
        print(json.dumps({"ok": False, "error": str(e)}))
        return 1
    summary["ok"] = True
    print(json.dumps(summary))
    return 0


if __name__ == "__main__":
    sys.exit(main())
