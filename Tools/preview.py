#!/usr/bin/env python3
"""Local, fixed-origin WebGL launcher. No external network binding."""
from pathlib import Path
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
import argparse
import functools
import urllib.request
import webbrowser
import sys

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument("--port", type=int, default=8920)
parser.add_argument("--no-open", action="store_true")
args = parser.parse_args()
if not 1 <= args.port <= 65535:
    parser.error("port must be 1..65535")
webroot = root / "Build" / "WebGL"
if not (webroot / "index.html").exists():
    webroot = root / "WebGL"
if not (webroot / "index.html").exists():
    sys.exit("WebGL build missing. Build with Tools/build.sh web first.")
url = f"http://127.0.0.1:{args.port}"

class Handler(SimpleHTTPRequestHandler):
    extensions_map = {**SimpleHTTPRequestHandler.extensions_map,
                      ".wasm": "application/wasm", ".data": "application/octet-stream"}
    def end_headers(self):
        self.send_header("Cache-Control", "no-cache")
        super().end_headers()

try:
    server = ThreadingHTTPServer(("127.0.0.1", args.port),
                                functools.partial(Handler, directory=str(webroot)))
except OSError:
    try:
        with urllib.request.urlopen(url + "/index.html", timeout=2) as response:
            existing = response.read()
        if existing != (webroot / "index.html").read_bytes():
            raise RuntimeError("different content")
    except Exception:
        sys.exit(f"Port {args.port} is occupied by another service. Stop it or specify --port.")
    if not args.no_open:
        webbrowser.open(url)
    print("The same game is already served at", url)
    sys.exit(0)
print("Mist Lantern:", url, flush=True)
if not args.no_open:
    webbrowser.open(url)
try:
    server.serve_forever()
except KeyboardInterrupt:
    pass
finally:
    server.server_close()
