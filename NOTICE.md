
### Changes made in the `feature` branch:

- Added a new feature for generating a `png` image of a symbol, with the option to replace the `MaterialSymbol` component with an `Image` component, setting its sprite property to the generated symbol's image.
- Refactored existing scripts and `asmdef` files to improve code quality, including:
    - Fixing inconsistent identifier names.
    - Using the `var` keyword for obvious types.
    - Converting most fields to properties.
    - Following clean code principles throughout.
- Simplified the package directory structure, making it more intuitive. The demo scene is now accessible through the Package Manager's **Samples** tab.
- Added a convenient menu option to quickly create a new symbol and cleanly uninstall the package.
- Fixed an issue where creating a new symbol would incorrectly initialize a new UI canvas instead of nesting it within an existing one (prioritizing existing canvases).

---

### Why this Image Generation Feature?

**TL;DR**:
To improve batching performance in Unity, I added an image generation feature to convert Material Symbols into images. This helps maintain batching even when symbols overlap with other UI elements (like images), reducing draw calls and boosting performance. You can also group the generated images into a sprite atlas for even better batching efficiency.

---

I've always been a fan of using Google's Material Symbols in my Unity projects. Typically, I download the symbol or icon I need with a desired size, but this process became repetitive and time-consuming. To streamline my workflow, I started looking for a more efficient solution. That's when I discovered this package—it already had most of what I needed (even without the image generation feature), and I began using it extensively in my projects.

While finalizing a particular project, I noticed an issue with batching performance. Unity wasn't properly batching the symbols alongside other UI elements. In my UI, I used a mix of TextMeshPro (TMPro) texts, images, and material symbols. Here’s how batching should ideally work:

- 1 TMPro text = 1 draw call
- 1 image = 1 draw call
- 1 material symbol = 1 draw call

Even if I have multiple instances of each (e.g., 4 texts, 5 identical images, 3 material symbols), it should still result in just 3 draw calls, thanks to Unity's batching of similar elements. However, this only works properly if the UI structure and arrangement are optimal.

The problem arose when I overlapped a material symbol with an image—batching would break. For instance, consider a play button with a rectangular background image (for the clickable area) and a foreground symbol image (bearing a play symbol). In this scenario, the overlap caused unnecessary draw calls.

To resolve this, I implemented an image generation feature that converts material symbols into images. This way, even when overlapping with other UI images, batching remains intact. To further optimize performance, you can group these images into a sprite atlas to maximize batching efficiency.

---

This project is licensed under the [Apache License 2.0](https://github.com/ebukaracer/UnityMaterialSymbols?tab=Apache-2.0-1-ov-file).  

Credits to the original developer for laying a massive foundation.