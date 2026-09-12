# Artwork

The banner and package icon use the existing hammer artwork from
`assets/unity/PlanBuild/Assets/PlanBuild/Icons/plan_hammer.png`, inherited from PlanBuild.
The SVG sources embed that image and use a flat forest-green background.

The banner lettering uses DejaVu Serif Bold and DejaVu Sans, converted to paths
for consistent rendering. Font notices are in `FONT-LICENSE.txt`.

Render the PNG assets from the repository root with librsvg:

```sh
rsvg-convert assets/artwork/banner.svg -o package/banner.png
rsvg-convert assets/artwork/icon.svg -o package/icon.png
```
