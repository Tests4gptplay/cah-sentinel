# Synthetic GIF fixture

`disposal.gif` is original test data: four 3×2 indexed-color frames with red,
green, blue and yellow pixels, transparency, 100/200/300/400ms delays,
and disposal methods 1/2/3/1. It deliberately contains an infinite loop extension;
the player must still enforce COMPLETE PlayOnce. No character artwork is used.

`disposal.expected.json` records independently decoded Pillow 12.3.0 full-canvas
BGRA bytes (transparent RGB normalized to zero). The Windows test compares every
pixel of every WPF-composed frame against this oracle, not just file headers.
These synthetic fixtures are project-owned AGPL-3.0-only test material.
