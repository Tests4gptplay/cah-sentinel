param([string]$FFmpeg = 'ffmpeg')
$ErrorActionPreference = 'Stop'
$assets = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../assets/helper'))
$filter = "[0:v]format=rgba,scale=w=338:h='338+2*sin(2*PI*n/48)':flags=lanczos:eval=frame[pet];[1:v][pet]overlay=x=41:y='415-overlay_h':format=auto:shortest=1,split[p0][p1];[p0]palettegen=reserve_transparent=1:transparency_color=0x000000:stats_mode=full[pal];[p1][pal]paletteuse=dither=bayer:bayer_scale=3:alpha_threshold=128[out]"
& $FFmpeg -hide_banner -loglevel error -loop 1 -framerate 8 -i (Join-Path $assets 'source.png') -f lavfi -i 'color=c=black@0.0:s=420x420:r=8:d=6,format=rgba' -filter_complex $filter -map '[out]' -frames:v 48 -loop 0 -gifflags 0 -y (Join-Path $assets 'loop.gif')
if ($LASTEXITCODE -ne 0) { throw 'Helper GIF encoding failed.' }