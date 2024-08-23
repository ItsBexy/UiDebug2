An updated version of FFXIVUiDebug, based on the original version by aers (https://github.com/aers/FFXIVUIDebug) and Caraxi's fork (https://github.com/Caraxi/SimpleTweaksPlugin/blob/main/Debugging/UIDebug.cs)

This repo builds the tool as a standalone Dalamud plugin.

### Feature updates
- Any addon or node can now pop out into its own window.
- Revised the visual style of node field/property information
- Color values are now visually displayed
- Any nodes or components that are referenced by fields within the addon will now show that field name in the inspector.
- Added editors for nodes, allowing complete control over most of their properties.
- Improved texture display for Image nodes (and Image node variant types). The active part of the texture is now highlighted, and the boundaries of other parts can be shown via mouseover
- Highlighting of node bounds onscreen is now more accurate, factoring in rotation (including when using the Element Selector)
- Display of animation timelines has been revamped, showing a table of keyframes for each animation.
