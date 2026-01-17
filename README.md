# ZUI - Modern UI Framework for V Rising

A powerful, modular UI framework for V Rising that provides an intuitive API for creating custom interfaces and integrating with game systems.  This mod was created using BloodCraftUI - OnlyFams as a template base.  So this mod has built in interactions between most of BloodCraft systems, also it has built-in features for KinPonds & ScarletSigns.

---

## ✨ Features

- **Customizable Themes** - Dynamic theming system with opacity controls and visual customization
- **Resizable Panels** - Drag-and-drop panel system with persistent positioning and sizing
- **Plugin Registry** - Easy registration system for external mods to add custom buttons and commands
- **Event-Driven Architecture** - Reactive UI updates with built-in event handling
- **Accessibility** - Clean, intuitive interfaces designed for ease of use
- **Performance Optimized** - Efficient rendering and update systems for smooth gameplay

---

## How to Install

Place ZUI.dll and the Sprites folder (the folder itself along with its contents) into your plugins folder.

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

### Example API Hooks

| Hook | Description | Usage |
|------|-------------|-------|
| `ZUI.SetPlugin(string)` | Sets the plugin context for registrations | `ZUI.SetPlugin("MyMod");` |
| `ZUI.AddCategory(string)` | Creates a category under the current plugin | `ZUI.AddCategory("Admin");` |
| `ZUI.AddButton(string, string, string)` | Registers a button with command | `ZUI.AddButton("Text", ".cmd", "tooltip");` |

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

- **Panthernet** - BloodCraftUI - OnlyFams was used as a base template & I am using his ModernUI.
- **Flaticon.com** - Resize image icon

---

## 📄 License

This product is licensed under LGPLv3, but the base plugin 'BloodCraftUI - OnlyFams' is registered under MIT, so therefor this is a dual-license between MIT and my works which is LGPLv3.

See [LICENSE.LGPLv3](LICENSE.LGPLv3) for LGPLv3 details and [LICENSE.mit](LICENSE.mit) for MIT details.