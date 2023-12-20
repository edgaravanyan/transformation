# GPU Morphing Demo

A Unity project showcasing a GPU-powered transformation effect on a mesh with one million cubes spread over its surface.

## Overview

This project demonstrates the use of GPU computing to create a morphing effect between different 3D models. The transformation is visualized by distributing one million cubes over the surface of the given mesh.

## Features

- GPU-powered transformation effect on a mesh.
- One million cubes spread over the surface of a given mesh.
- Flexible and customizable morphing parameters.
- Unity project structure for easy integration and extension.

## Demo Video

[Demo](https://edavanyan.github.io/portfolio/images/demo_transformations.mp4)

## Getting Started

### Prerequisites

- Unity 2020.3.1f1 or later

### Installation

1. Clone the repository:

   ```bash
   git clone https://github.com/YourUsername/GPUMorphingDemo.git
   ```
2. Open the project in Unity

# Usage

- Open the scene: `Assets/Scenes/MorphingDemoScene.unity`.
- Play the scene to experience the GPU-powered morphing effect.

## Project Structure

- `Assets/Scripts/GPUShapeOfModel.cs`: GPU compute shader for morphing between different 3D models.
- `Assets/Scripts/CubeGridGenerator.cs`: Unity script for generating a grid of cubes over a mesh surface.
- `Assets/Materials/MorphingMaterial.mat`: Material for rendering the morphing effect.
- `Screenshots/`: Directory containing screenshots and demo GIFs.

## Example

```csharp
// Example code snippet demonstrating how to use GPU morphing.
// Customize the parameters based on your requirements.

void Start()
{
    // Set up GPU morphing parameters
    GPUShapeOfModel gpuMorphing = GetComponent<GPUShapeOfModel>();
    gpuMorphing.SetMeshes(meshList);  // List of 3D models for morphing
    gpuMorphing.SetResolution(100);   // Resolution of the morphing effect
    gpuMorphing.StartMorphing();      // Trigger the morphing effect
}
```
## License

This project is licensed under the [MIT License](https://opensource.org/licenses/MIT).


