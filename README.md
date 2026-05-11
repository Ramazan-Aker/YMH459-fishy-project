# AR Fish Museum - 3D Modeling and Interactive Application

## About the Project
This project is an Augmented Reality (AR) based Fish Museum application developed using the Unity engine. It aims to transform traditional museum visits into an immersive digital experience. Users can visualize, interact with, and learn about various fish species by placing high-quality 3D models onto real-world surfaces using mobile devices. 

The application leverages modern AR technologies to bridge the gap between static exhibits and dynamic educational content.

## Key Features
* **Markerless AR Placement:** Detect real-world surfaces (floors, tables) using ARCore and place 3D fish models seamlessly.
* **Interactive 3D Visualization:** High-quality, low-poly models optimized for mobile performance.
* **Educational Modules:** Dynamic UI panels displaying detailed information such as species name, habitat, and dietary habits.
* **Spatial Audio:** Immersive voice-over narrations and underwater sound effects for an enhanced learning experience.
* **Smooth Navigation:** Intuitive touch-based camera controls and interaction systems designed for mobile users.
* **Persistence System:** (Planned) Save and load system for custom modeling configurations and material edits.

## Technology Stack
* **Vuforia:** (for augmented reality tracking and image recognition)
* **Unity UI:** UI Toolkit (for dynamic UI elements)
* **Programming Language:** C# (.NET Framework 4.7.1+)
* **3D Modeling:** Blender (for optimized low-poly assets)
* **Version Control:** Git

## Project Structure
```text
YMH459-fishy-project/
│
├── Assets/
│   ├── Scripts/            # C# Script Files
│   │   ├── Modeling/       # 3D Modeling logic
│   │   ├── UI/             # User Interface controllers
│   │   ├── Camera/         # Camera control & Raycasting
│   │   └── Utils/          # Helper tools & Extensions
│   │
│   ├── Scenes/             # Unity Scene files (Main, AR, UI)
│   ├── Models/             # 3D Asset files
│   │   ├── Fish/           # Optimized fish models
│   │   └── Environment/    # AR environment assets
│   │
│   ├── Materials/          # PBR Materials
│   ├── Textures/           # Diffuse, Normal, and AO maps
│   ├── Prefabs/            # Reusable GameObject templates
│   ├── UI/                 # UI Sprites and Fonts
│   └── Audio/              # SFX and Voice-over clips
│
├── ProjectSettings/        # Unity Project Configuration
├── Documentation/          # Technical specifications and reports
└── README.md