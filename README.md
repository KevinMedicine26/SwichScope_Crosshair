# SwitchScope Crosshair V3.1 Founder Deluxe Edition

A Windows desktop application that displays customizable crosshair overlays on your screen for enhanced aiming precision in games and other applications.

## 📋 Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Usage](#usage)
- [Image Pack](#image-pack)
- [Hotkeys](#hotkeys)
- [Technical Specifications](#technical-specifications)
- [Technical Principles](#technical-principles)
- [System Requirements](#system-requirements)
- [Configuration](#configuration)
- [Building from Source](#building-from-source)

## ✨ Features

### Core Functionality
- **Multiple Crosshair Support**: Load and switch between up to 3 different crosshair images
- **Real-time Switching**: Quick crosshair switching using customizable hotkeys
- **Transparent Overlay**: Click-through crosshair that doesn't interfere with gameplay
- **Always On Top**: Crosshair stays visible above all other windows
- **Precise Positioning**: Manual position adjustment with pixel-level precision
- **Dynamic Sizing**: Scale crosshairs from 50% to 400% of original size
- **Center Alignment**: One-click centering to screen center

### Advanced Features
- **Persistent Settings**: Automatic saving and loading of configurations
- **Individual Positioning**: Each crosshair remembers its position and size
- **Default Folder Management**: Customizable default crosshair image directory
- **Auto-Recovery**: Automatically creates default crosshair if none available
- **Window State Management**: Maintains topmost status and transparency

## 🚀 Installation

### Prerequisites
- Windows 10/11 (64-bit)
- .NET 8.0 Runtime (Windows Desktop)

### Quick Start
1. Download the latest release from the releases page
2. Extract the application files to your desired location
3. Run `crosshair3.exe`
4. The application will automatically create a default crosshair folder in `Documents\Crosshairs`

## 📖 Usage

### Basic Operation

1. **Launch Application**: Run `crosshair3.exe`
2. **Load Crosshairs**: Use "Select Crosshair 1/2/3" buttons to load PNG images
3. **Activate**: Click the "Activate" button to show the crosshair overlay
4. **Position**: Use the position controls to fine-tune crosshair placement
5. **Switch**: Use the configured hotkey (default: `0`) to cycle between loaded crosshairs

### Control Interface

#### Main Controls
- **Activate/Deactivate**: Toggle crosshair visibility
- **Select Crosshair 1/2/3**: Load images into crosshair slots
- **Center Crosshair**: Move crosshair to screen center
- **Size Slider**: Adjust crosshair scale (50%-400%)

#### Position Controls
- **X/Y Text Boxes**: Manual position input
- **+/- Buttons**: Fine position adjustment (1 pixel increments)
- **Position Display**: Shows current crosshair coordinates

#### Settings
- **Hotkey Configuration**: Set custom switching hotkey
- **Default Folder**: Manage crosshair image directory
- **Switch Test**: Test hotkey functionality

### Workflow Example
```
1. Launch application
2. Click "Select Crosshair 1" → Choose your preferred crosshair PNG
3. Click "Select Crosshair 2" → Choose alternative crosshair PNG
4. Click "Activate" to display crosshair
5. Click "Center Crosshair" for perfect screen alignment
6. Adjust size with slider if needed
7. Use hotkey (default: 0) to switch between crosshairs during gameplay
```

## 🎯 Image Pack

### Included Crosshairs
The application comes with a curated collection of crosshair images located in:
```
/crosshair3/image/crosshair_png_package/
```

#### Available Styles:
- **`standard1.png`** - Classic dot crosshair
- **`arrow3.2_L.png`** - Arrow-style crosshair (Large)
- **`arrow3N.png`** - Arrow-style crosshair (Normal)
- **`longbow4.png`** - Extended line crosshair
- **`longbow4N_L.png`** - Extended line crosshair (Large)
- **`Tshape2.png`** - T-shaped crosshair
- **`Tshape2L.png`** - T-shaped crosshair (Large)

### Custom Crosshairs
- **Supported Format**: PNG files with transparency
- **Recommended Size**: 16x16 to 64x64 pixels for optimal performance
- **Design Tips**: Use high contrast colors for visibility
- **Transparency**: Utilize PNG alpha channel for clean overlay appearance

### Default Folder Structure
```
Documents/
└── Crosshairs/
    ├── default_crosshair.png (auto-generated)
    └── [your custom crosshairs]
```

## ⌨️ Hotkeys

### Default Hotkey
- **Switch Crosshair**: `0` (Zero key)

### Customization
1. Click "Set Switch Hotkey" button
2. Press desired key when prompted
3. Key will be registered system-wide
4. Settings are automatically saved

### Supported Keys
- Number keys (0-9)
- Letter keys (A-Z)
- Function keys (F1-F12)
- Special keys (Insert, Delete, etc.)

## 🔧 Technical Specifications

### Technology Stack
- **Framework**: .NET 8.0 WPF (Windows Presentation Foundation)
- **Language**: C# 12.0
- **UI Framework**: XAML
- **Target Platform**: Windows (net8.0-windows)

### Dependencies
- **Microsoft.NET.Sdk**: Core SDK
- **System.Windows.Forms**: Folder browser integration
- **Windows API**: Low-level window management

### Application Architecture
```
crosshair3/
├── MainWindow.xaml              # UI Layout
├── MainWindow.xaml.cs          # Core Logic (899 lines)
├── App.xaml                    # Application Resources
├── App.xaml.cs                 # Application Entry Point
├── crosshair3.csproj           # Project Configuration
└── Resources/
    └── SwitchScope_CrossHair_Icon.ico
```

## 🛠️ Technical Principles

### Window Management System

#### Overlay Implementation
The crosshair uses advanced Windows API techniques for optimal overlay functionality:

```csharp
// Layered Window with Click-Through
int exStyle = GetWindowLong(handle, GWL_EXSTYLE);
exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
SetWindowLong(handle, GWL_EXSTYLE, exStyle);
```

#### Key Technical Features:

1. **Topmost Window Management**
   - Uses `SetWindowPos` with `HWND_TOPMOST` flag
   - Implements 25-second refresh timer to maintain topmost status
   - Handles window focus events to preserve overlay position

2. **Click-Through Technology**
   - `WS_EX_TRANSPARENT` flag enables mouse click pass-through
   - `WS_EX_LAYERED` flag enables transparency and alpha blending
   - Combination allows overlay without blocking user interaction

3. **Global Hotkey System**
   - Registers system-wide hotkeys using `RegisterHotKey` API
   - Handles `WM_HOTKEY` messages through Windows message loop
   - Automatic cleanup on application shutdown

4. **Memory Management**
   - Efficient bitmap caching and loading
   - Automatic resource disposal
   - Optimized image scaling algorithms

### Data Persistence Architecture

#### Settings Management
```csharp
public class CrosshairSettings
{
    public string[] CrosshairPaths { get; set; } = new string[3];
    public int LastUsedIndex { get; set; } = 0;
    public double[] WindowLeftPositions { get; set; } = new double[3];
    public double[] WindowTopPositions { get; set; } = new double[3];
    public double SizeValue { get; set; } = 100;
    public bool IsActive { get; set; } = true;
    public string DefaultCrosshairFolder { get; set; }
}
```

#### Configuration Storage
- **Location**: `%AppData%\CrosshairOverlay\settings.xml`
- **Format**: XML serialization
- **Auto-save**: Settings saved on every change
- **Recovery**: Automatic default generation if settings corrupted

### Image Processing Pipeline

1. **Loading Phase**
   - PNG file validation and loading
   - Automatic size detection and optimization
   - Error handling for corrupted images

2. **Rendering Phase**
   - WPF Image control with hardware acceleration
   - Real-time scaling with maintained aspect ratio
   - Smooth interpolation for quality preservation

3. **Fallback System**
   - Programmatic crosshair generation if no images available
   - Default crosshair creation using pixel manipulation
   - Automatic folder structure creation

## 💻 System Requirements

### Minimum Requirements
- **OS**: Windows 10 (Version 1809 or later)
- **Architecture**: x64 (64-bit)
- **Memory**: 512 MB RAM
- **Storage**: 50 MB available space
- **Framework**: .NET 8.0 Desktop Runtime

### Recommended Requirements
- **OS**: Windows 11
- **Memory**: 1 GB RAM
- **Storage**: 100 MB available space (for custom crosshairs)
- **Display**: 1920x1080 or higher resolution

## ⚙️ Configuration

### Settings File Location
```
%AppData%\CrosshairOverlay\settings.xml
```

### Default Crosshair Directory
```
%UserProfile%\Documents\Crosshairs\
```

### Manual Configuration
Advanced users can directly edit the settings XML file for bulk configuration changes.

## 🏗️ Building from Source

### Prerequisites
- Visual Studio 2022 (17.8 or later)
- .NET 8.0 SDK
- Windows 10 SDK

### Build Steps
```bash
# Clone the repository
git clone [repository-url]
cd crosshair3

# Restore dependencies
dotnet restore

# Build application
dotnet build --configuration Release

# Run application
dotnet run
```

### Output Location
```
/bin/Release/net8.0-windows/
├── crosshair3.exe
├── crosshair3.dll
└── [runtime dependencies]
```

---

## 📄 License

This project is part of the SwitchScope Crosshair V3.1 Founder Deluxe Edition.

## 🤝 Support

For technical support or feature requests, please refer to the application's built-in help system or contact the development team.

---

**Version**: 3.1 Founder Deluxe Edition  
**Last Updated**: 2024  
**Compatibility**: Windows 10/11 (.NET 8.0) 