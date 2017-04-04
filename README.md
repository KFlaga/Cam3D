# Cam3D
Application for creating 3D images from stereovision

Core purpose of the project is creating maps of 3d points from images taken by indoor stereovision system (pair of cameras with fixed relative position/orientation) and their visualization.
Project started as hobby project, presently used for my bachelor thesis.

I've included all steps necessary for 3d reconstruction:
- images capture (with DirectShow)
- calibration points extraction (with predefined calibration grid pattern)
- radial distortion reduction (using model by [3] + custom model's parameters estimation method)
- camera calibration (using modified 'Gold Standard'  algorithm [1] optimized for calibration grids data model)
- image rectification (using slightly modified method by [6] and [7])
- image matching (using Semi-Global Matching [4][5] for dense matching)
- disparity map refinements (in form of sequential refinement steps, mainly using  [4][9])
- 3d points triangulation ( two points method [1] )
- 3d visualization (with custom mini rendering engine based on DirectX)
- additional auxiliary image processing algorithms
Most of algorithms used are my implementations based on published academic works, mostly working as expected for real images, although some minor tweaks may be still needed.

Uses WPF for GUI and Xml serialization for storing most of input or processed data. 

