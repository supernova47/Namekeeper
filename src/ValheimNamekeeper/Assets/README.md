# Handwritten sign assets

Font: Marck Script, original unmodified MarckScript-Regular.ttf from https://github.com/google/fonts/tree/main/ofl/marckscript . Cyrillic and Latin supported; SIL OFL 1.1 is included at project root FONT_LICENSE.txt.

Rebuild sign_ru.png / sign_en.png with `python tests/GenerateSignAssets.py` (Pillow required only for this development step). Current generation uses Pillow 11.1.0. Transparent 1024x256 RGBA images contain nearly black ink; these two PNGs are embedded into the mod DLL. No OS font dependency exists at runtime.
