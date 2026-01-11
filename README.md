# ZUI - Modern UI Framework for V Rising

> **Credits**: Panthernet - BloodCraftUI - OnlyFams was used as a base template

A powerful, modular UI framework for V Rising that provides an intuitive API for creating custom interfaces and integrating with game systems.

---

## ✨ Features

- **Modular Design** - Component-based architecture for building flexible and reusable UI elements
- **Customizable Themes** - Dynamic theming system with opacity controls and visual customization
- **Resizable Panels** - Drag-and-drop panel system with persistent positioning and sizing
- **Plugin Registry** - Easy registration system for external mods to add custom buttons and commands
- **Event-Driven Architecture** - Reactive UI updates with built-in event handling
- **Sprite Loading** - Simple API for loading custom sprites and icons from your plugin directory
- **State Management** - Built-in services for managing game state and dependencies
- **Hot Reload Support** - Dynamic UI updates without requiring game restarts
- **Accessibility** - Clean, intuitive interfaces designed for ease of use
- **Performance Optimized** - Efficient rendering and update systems for smooth gameplay

---

## 📚 API Hooks & How to Use

### Basic Setup

Initialize ZUI in your plugin and start registering UI elements:

```csharp
using ZUI.API;

// Set your plugin context
ZUI.SetPlugin("MyAwesomeMod");

// Add a category for organization
ZUI.AddCategory("Player Commands");

// Register buttons that execute commands
ZUI.AddButton("Heal", ".heal", "Heals the player to full health");
ZUI.AddButton("Teleport Home", ".tp home", "Teleports you to your spawn point");
```

### Creating Panels

```csharp
using ZUI.UI.CustomLib.Panel;
using ZUI.UI.UniverseLib.UI;

public class MyCustomPanel : ResizablePanelBase
{
    public MyCustomPanel(UIBase owner) : base(owner) { }
    
    public override string Name => "My Custom Panel";
    public override int MinWidth => 300;
    public override int MinHeight => 200;
    
    protected override void ConstructPanelContent()
    {
        base.ConstructPanelContent();
        // Add your custom UI elements here
    }
}
```

### Loading Custom Sprites

```csharp
using ZUI;

// Load a sprite from your plugin's Sprites directory
// Place images in: BepInEx/plugins/{YourPlugin}/Sprites/{filename}
var myIcon = Plugin.LoadSprite("icon.png", 100f);
```

### Example API Hooks

| Hook | Description | Usage |
|------|-------------|-------|
| `ZUI.SetPlugin(string)` | Sets the plugin context for registrations | `ZUI.SetPlugin("MyMod");` |
| `ZUI.AddCategory(string)` | Creates a category under the current plugin | `ZUI.AddCategory("Admin");` |
| `ZUI.AddButton(string, string, string)` | Registers a button with command | `ZUI.AddButton("Text", ".cmd", "tooltip");` |
| `ZUI.RemoveButton(string)` | Removes a registered button | `ZUI.RemoveButton("Text");` |
| `ZUI.RemovePlugin(string)` | Removes entire plugin registration | `ZUI.RemovePlugin("MyMod");` |
| `Plugin.LoadSprite(string, float)` | Loads a sprite from plugin directory | `Plugin.LoadSprite("icon.png", 100f);` |

### Advanced Registration Example

```csharp
// Setup plugin with multiple categories
ZUI.SetPlugin("AdvancedMod");

// Player category
ZUI.AddCategory("Player");
ZUI.AddButton("Full Heal", ".heal max");
ZUI.AddButton("Speed Boost", ".buff speed");

// Admin category
ZUI.AddCategory("Admin");
ZUI.AddButton("God Mode", ".god");
ZUI.AddButton("Spawn Items", ".spawn all");

// Listen for changes
ZUI.OnButtonsChanged += () => {
    Console.WriteLine("UI buttons updated!");
};
```

---

## 🙏 Credits

- **Panthernet** - BloodCraftUI - OnlyFams was used as a base template
- **Flaticon.com** - Resize image icon
- **ZUI Contributors** - For ongoing development and improvements

---

## 📄 License

This product is licensed under LGPLv3, but the base mod/plugin 'BloodCraftUI - OnlyFams' is registered under MIT, so therefor this is a dual-license between MIT and my works which is LGPLv3.

See [LICENSE.txt](LICENSE.txt) for LGPLv3 details and [LICENSE.mit](LICENSE.mit) for MIT details.