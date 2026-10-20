#!/bin/zsh
cd "${0:A:h}"
if [[ ! -f Tools/preview.py ]]; then cd ..; fi
exec python3 Tools/preview.py
