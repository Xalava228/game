# Production artwork

The 26 character/boss PNGs and logo in raster/ were generated with the built-in imagegen tool for this project. Each exact prompt is retained in prompts/. These are original generated assets, not downloaded stock art. The supplied character references informed the time-thief theme. PNGs contain an actual alpha channel; no checkerboard background is baked in.

The artwork uses indigo, ivory, antique gold and mint. ui-art.cjs authors the compatible background and nine-slice frame as SVG, and exports the generated logo at 512 px for the loader. Ordinary character textures use a 512 px import ceiling; bosses and the large menu witch use 1024 px. Original 1254 px PNGs are retained for later changes. Mipmaps and trilinear sampling reduce shimmer when scaled down.

art.cjs and boss-art.cjs retain their older vector concepts for reproducibility, but prefer production PNGs when present. Running either script does not replace the painted cast with vector placeholders.

Alegreya SC Bold and Nunito Bold are from Google Fonts under SIL OFL; source font files and license texts are in fonts/. Source for the added title font: https://github.com/google/fonts/tree/main/ofl/alegreyasc .

Working asset generation, caches and final files are stored on D:. The thread-specific Codex generated-image directory resolves through a junction to D:/Game/Work/GeneratedImages.
