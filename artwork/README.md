# Artwork

The banner and package icon use the existing hammer artwork from
`PlanBuildUnity/Assets/PlanBuild/Icons/plan_hammer.png`, inherited from PlanBuild.
The SVG sources embed that image and use a flat forest-green background.

The banner lettering uses DejaVu Serif Bold and DejaVu Sans, converted to paths
for consistent rendering. Font notices are in `FONT-LICENSE.txt`.

Render the PNG assets from the repository root with librsvg:

```sh
rsvg-convert artwork/banner.svg -o banner.png
rsvg-convert artwork/icon.svg -o icon.png
```
