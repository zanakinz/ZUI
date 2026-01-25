# ZUI - Modern UI Framework for V Rising

ZUI is a powerful, modular UI framework for V Rising that allows developers to create professional, interactive interfaces with minimal effort. Originally built upon the foundation of *BloodCraftUI - OnlyFams*, ZUI has evolved into a complete **Client-Server Hybrid UI engine** capable of spawning independent windows, rendering custom textures, streaming images from web URLs, playing audio, and providing deep integration with both client-side and server-side systems.

ZUI features built-in support for **BloodCraft**, **KinPonds**, **ScarletSigns**, and **KindredCommands**, while providing a robust API for third-party modders to build their own standalone tools. Server mods can now create UIs dynamically via chat packets without requiring players to install additional client mods.

---

## 🖼️ UI Preview

<p align="center">
  <img src="https://zanakinz.github.io/ZUI/Images/UIPreview.png" alt="ZUI Interface Preview" width="800">
</p>

<sub>*Note: ZUIExampleMod & ScarletSigns are not included with this package. Please download them individually.*</sub>

---

## ✨ Key Features

- **Client-Server Hybrid Architecture**: Use ZUI from client mods OR server mods via JSON chat packets
- **Server-Side Mod Support**: Server mods can create UIs without requiring client-side mod installation
- **Dynamic Image Loading**: Stream images from web URLs (HTTP/HTTPS) with automatic caching
- **GIF Animation Support**: Load and display animated GIFs from web or local sources
- **Audio System**: Play sounds from web URLs or local files with volume control
- **Multi-Tab Panels**: Create organized interfaces with multiple tabs in custom windows
- **Custom Window Engine**: Create independent windows using pixel-perfect custom dimensions OR use the visual designer at [https://zanakinz.github.io/ZUI](https://zanakinz.github.io/ZUI) to create your own UI visually
- **Dynamic Content Registration**: Add categories, text blocks, and interactive buttons to any window on the fly
- **Custom Image Support**: Load and render PNG/JPG/GIF textures from your mod's Sprites folder OR from web URLs
- **Persistent Layouts**: All windows support dragging and resizing with automatic data saving; your UI stays exactly where you left it
- **Theming & 9-Slicing**: Professional visual style using 9-sliced sprites for seamless button and panel scaling
- **Reflection-based Integration**: Third-party mods can integrate without compile-time dependencies
- **Packet Protocol**: Hidden chat-based packet system allows server mods to control client UIs

---

## 🛠️ Installation

1. Download the latest release
2. Place `ZUI.dll` and the `Sprites` folder into your `BepInEx/plugins/` directory
    - *Note: The Sprites folder is required for the default framework visuals*
3. **Configuration**: By default, server-side integrations (like BloodCraft) are disabled. Enable them in the `ZUI.cfg` file if your server supports them

---

## 🔒 Security & Trust

ZUI implements multiple security measures to protect users, but **ultimate responsibility lies with you, the user**. Here's what you need to know:

### 🛡️ What ZUI Does to Protect You
- **Default-deny approach**: Server-initiated audio downloads are **disabled by default**
- **Explicit opt-in required**: You must manually enable `AllowServerAudioDownloads` in the config
- **Sandboxed execution**: UI code runs in a controlled environment

### ⚠️ What You Must Do
- **Trust your mods**: Only install ZUI-compatible mods from sources you trust
- **Verify external files**: Any mod that includes local audio files (in `Audio/` folder) or images (in `Sprites/` folder) should come from a trusted developer
- **Trusted servers only**: Only enable `AllowServerAudioDownloads` on servers you completely trust
- **Review what you download**: Server-streamed content (images, audio from URLs) comes from external sources - ensure you trust the server administrator

### 🚨 Key Security Settings

**`AllowServerAudioDownloads`** (in `ZUI.cfg`):
- **Default**: `false` (disabled)
- **Purpose**: Controls whether servers can initiate audio file downloads to your client
- **Risk**: Malicious servers could potentially download unwanted audio files
- **Recommendation**: Only enable this on private servers or servers run by people you trust

**Bottom line**: ZUI provides the tools and safeguards, but you control what content enters your game. Be thoughtful about which mods you install and which servers you connect to.

---

## 📚 Developer API & Examples

ZUI supports **two integration methods**: Client-Side (via reflection) and Server-Side (via chat packets).

### 📷 Method 1: Client-Side Integration (Reflection-Based)

For client-side mods, use ZUI's "Context" based API via reflection. You set the plugin and window you want to work on, then add your content.

### 📘 Basic Registration (Main Menu Integration)

By default, adding buttons without setting a custom UI context will place them in the global **Main** menu.

### 💡 Advanced Client-Side Example

```csharp
using ZUI.API;

// Best practice: Set your plugin name first
ZUI.SetPlugin("MyAwesomeMod");

// Target the main menu
ZUI.SetTargetWindow("Main");

// Add buttons to the default Main menu
ZUI.AddCategory("General Tools");
ZUI.AddButton("Heal Me", ".heal", "Restores your health");
ZUI.AddButton("Speed Boost", ".buff speed", "Temporary speed increase");
```

### 🎨 Custom Window Creation

Create completely custom UI windows with precise control over layout and positioning:

```csharp
// Initialize your plugin context
ZUI.SetPlugin("MyCustomMod");

// Target a custom window (creates it if it doesn't exist)
ZUI.SetTargetWindow("MyControlPanel");

// Set custom dimensions (width x height in pixels)
ZUI.SetUI(600, 400);

// Set the window title with color support
ZUI.SetTitle("<color=#FF6B6B>My Control Panel</color>");

// Add positioned text
ZUI.AddText("<color=#4ECDC4>Welcome to my mod!</color>", 20f, 50f);
ZUI.AddText("Version 2.0", 20f, 80f);

// Add positioned categories
ZUI.AddCategory("<color=#FFE66D>Settings:</color>", 20f, 120f);
ZUI.AddCategory("<color=#95E1D3>Actions:</color>", 320f, 120f);

// Add positioned buttons
ZUI.AddButton("Execute", ".mycommand", 20f, 160f);
ZUI.AddButton("Custom Action", ".action", 20f, 200f, 250f, 35f);  // With custom size

// Add images (from your mod's Sprites folder)
ZUI.AddImage("logo.png", 20f, 280f, 560f, 100f);
```

### 📑 Working with Tabs

Custom windows support multi-tab layouts for organizing complex interfaces:

```csharp
ZUI.SetPlugin("MyMod");
ZUI.SetTargetWindow("MyWindow");
ZUI.SetUI(700, 500);
ZUI.SetTitle("Multi-Tab Interface");

// Create tabs
ZUI.CreateTab("Home");
ZUI.CreateTab("Settings");
ZUI.CreateTab("Advanced");

// Add content to first tab (Home)
ZUI.AddText("Welcome to the home tab!", 20f, 20f);
ZUI.AddButton("Quick Action", ".action", 20f, 60f);

// Switch to Settings tab and add content
// Note: Content is added to the currently active tab in creation order
ZUI.CreateTab("Settings");
ZUI.AddCategory("Configuration", 20f, 20f);
ZUI.AddButton("Save Settings", ".save", 20f, 60f);

// Switch to Advanced tab
ZUI.CreateTab("Advanced");
ZUI.AddImage("advanced_logo.png", 50f, 50f, 200f, 100f);
```

**Important**: Tabs are only supported in Custom UI windows created with `SetUI()`. They cannot be used in the legacy Main menu.

### 🔊 Audio System

ZUI supports playing audio files from both local sources and web URLs, with full client-side and server-side support.

#### Client-Side Audio

```csharp
ZUI.SetPlugin("MyMod");

// Register a sound from a web URL
ZUI.RegisterSound("victory_sound", "https://yourdomain.com/sounds/victory.mp3");

// Play the registered sound
ZUI.PlaySound("victory_sound", 0.8f); // 80% volume

// Or play a local sound from your mod's Audio folder
ZUI.PlaySound("levelup.wav", 1.0f); // Full volume
```

**Local Audio Setup**: Place your audio files (`.wav`, `.mp3`, `.ogg`) in:
```
BepInEx/plugins/Audio/
```

#### Server-Side Audio (Packet-Based)

```csharp
// Register audio from URL
SendPacket("RegisterSound", new Dictionary<string, string> {
    { "Name", "server_notification" },
    { "Url", "https://yourdomain.com/sounds/notification.mp3" }
});

// Add a button that plays the sound when clicked
SendPacket("AddButton", new Dictionary<string, string> {
    { "Text", "Play Sound" },
    { "Cmd", ".zuiplay server_notification 0.5" }, // 50% volume
    { "X", "20" }, { "Y", "100" }
});
```

#### 🚨 IMPORTANT: Server Audio Security

Server-initiated audio downloads are **DISABLED by default** for your protection. To allow servers to download audio files to your client:

1. Open `BepInEx/config/Zanakinz.ZUI.cfg`
2. Find `AllowServerAudioDownloads` under `[GeneralOptions]`
3. Change from `false` to `true`

**⚠️ Security Warning**: Only enable this setting on servers you completely trust. Malicious servers could potentially download unwanted audio files to your client. When disabled, server audio packets will be ignored and logged.

### 🎞️ Enhanced Image Support (GIFs & Animations)

ZUI now supports animated GIFs in addition to static PNG/JPG images:

```csharp
// Local GIF from your mod's Sprites folder
ZUI.AddImage("dancing.gif", 100f, 50f, 300f, 200f);

// Or via server packets from web URL
SendPacket("RegisterImage", new Dictionary<string, string> {
    { "Name", "animated_banner" },
    { "Url", "https://yourdomain.com/images/banner.gif" }
});

SendPacket("AddImage", new Dictionary<string, string> {
    { "Img", "animated_banner" },
    { "X", "50" }, { "Y", "50" },
    { "W", "400" }, { "H", "200" }
});
```

GIFs will automatically play and loop once loaded. Supported formats: PNG, JPG, GIF.

### 📋 API Reference

| Method | Description | Usage |
|--------|-------------|-------|
| `SetPlugin(string)` | Sets the plugin identifier for UI registration | `ZUI.SetPlugin("MyMod");` |
| `SetTargetWindow(string)` | Targets "Main" menu or a custom window name | `ZUI.SetTargetWindow("Main");` |
| `SetUI(string)` | Creates custom window using a template name | `ZUI.SetUI("TemplateName");` |
| `SetUI(int, int)` | Creates custom window with width × height dimensions | `ZUI.SetUI(500, 350);` |
| `SetTitle(string)` | Sets window title (supports Unity Rich Text color tags) | `ZUI.SetTitle("<color=#FF0000>My Window</color>");` |
| `HideTitleBar()` | Hides the title bar of a custom window | `ZUI.HideTitleBar();` |
| `CreateTab(string)` | Creates a new tab in a custom window | `ZUI.CreateTab("Settings");` |
| `CreateTab(string, string)` | Creates a new tab with tooltip | `ZUI.CreateTab("Settings", "Config options");` |
| `AddCategory(string)` | Adds category label in main menu | `ZUI.AddCategory("Admin");` |
| `AddCategory(string, float, float)` | Adds category label at specific X, Y position | `ZUI.AddCategory("README:", 15f, 190f);` |
| `AddButton(string, string)` | Adds button to main menu with text and command | `ZUI.AddButton("Heal", ".heal");` |
| `AddButton(string, string, string)` | Adds button with text, command, and tooltip | `ZUI.AddButton("Heal", ".heal", "Restores HP");` |
| `AddButton(string, string, float, float)` | Adds button at X, Y position | `ZUI.AddButton("Test", ".cmd", 320f, 220f);` |
| `AddButton(string, string, float, float, float, float)` | Adds button with position (X, Y) and size (W, H) | `ZUI.AddButton("Long", ".cmd", 20f, 280f, 460f, 20f);` |
| `AddButton(string, string, string, float, float, float, float)` | Adds button with custom image background | `ZUI.AddButton("Text", ".cmd", "bg.png", 20f, 50f, 200f, 40f);` |
| `AddButtonWithCallback(string, Action)` | Adds button with C# callback function (no tooltip) | `ZUI.AddButtonWithCallback("Click", () => DoSomething());` |
| `AddButtonWithCallback(string, Action, string)` | Adds button with callback and tooltip | `ZUI.AddButtonWithCallback("Click", () => DoSomething(), "Tooltip");` |
| `AddButtonWithCallback(string, Action, float, float)` | Adds positioned button with callback | `ZUI.AddButtonWithCallback("Click", () => DoSomething(), 50f, 100f);` |
| `AddText(string, float, float)` | Adds text at specific X, Y coordinates | `ZUI.AddText("Hello!", 15f, 210f);` |
| `AddImage(string, float, float, float, float)` | Adds image (PNG/JPG/GIF) with filename, X, Y, width, height | `ZUI.AddImage("logo.png", 20f, 40f, 460f, 150f);` |
| `AddCloseButton(string)` | Adds a close button to a custom window | `ZUI.AddCloseButton("Close");` |
| `AddCloseButton(string, float, float)` | Adds a close button at X, Y position | `ZUI.AddCloseButton("Exit", 250f, 400f);` |
| `RemoveButton(string)` | Removes a button by its text label | `ZUI.RemoveButton("Heal");` |
| `RemoveElement(string)` | Removes any UI element by its ID | `ZUI.RemoveElement("elementId");` |
| `RemovePlugin(string)` | Removes all UI elements for a plugin | `ZUI.RemovePlugin("MyMod");` |
| `ClearAll()` | Clears all registered plugins and UI elements | `ZUI.ClearAll();` |
| `GetPlugins()` | Returns read-only list of all registered plugins | `var plugins = ZUI.GetPlugins();` |
| `RegisterSound(string, string)` | Registers audio from web URL with unique name | `ZUI.RegisterSound("ding", "https://url.com/sound.mp3");` |
| `PlaySound(string)` | Plays registered sound or local file at full volume | `ZUI.PlaySound("ding");` |
| `PlaySound(string, float)` | Plays registered sound or local file with volume (0.0-1.0) | `ZUI.PlaySound("ding", 0.8f);` |
| `OnButtonsChanged` | Event triggered when UI buttons are updated | `ZUI.OnButtonsChanged += () => { };` |

**Server-Side Only (Packet Methods):**
| Method | Description | Usage |
|--------|-------------|-------|
| `RegisterImage(string, string)` | Registers an image from a web URL | `SendPacket("RegisterImage", ...)` |
| `SetUICustom(int, int)` | Creates custom window via packet | `SendPacket("SetUICustom", ...)` |
| `Open()` | Forces a window to open | `SendPacket("Open", ...)` |

> **⚠️ Note**: Not all methods listed above may be fully functional. Some features are still in development (WIP) and may not work as intended. Refer to the **Experimental Features** section for details on known WIP functionality.

### 📶 Method 2: Server-Side Integration (Packet-Based)

Server mods can create UIs by sending specially formatted chat messages that ZUI intercepts and processes. **No client mod installation required** - players only need ZUI installed.

#### Packet Format

All packets follow this structure:
```
[[ZUI]]{"Type":"MethodName","Plugin":"YourPlugin","Window":"WindowName","Data":{...}}
```

#### Server-Side Helper Method

```csharp
using System.Collections.Generic;
using System.Text.Json;

private void SendPacket(string type, Dictionary<string, string> data, string plugin = "MyServerMod", string window = "ServerUI")
{
    var packet = new
    {
        Type = type,
        Plugin = plugin,
        Window = window,
        Data = data
    };

    string json = JsonSerializer.Serialize(packet);
    string message = "[[ZUI]]" + json;
    
    // Send to player via your server's chat system
    ServerChatUtils.SendSystemMessageToUser(userEntity, message);
}
```

#### Example: Creating a Server-Side UI with Tabs and Audio

```csharp
// 1. Set up the window
SendPacket("SetPlugin", new Dictionary<string, string> { { "Plugin", "MyServerMod" } });
SendPacket("SetTargetWindow", new Dictionary<string, string> { { "Window", "AdminPanel" } });
SendPacket("SetUICustom", new Dictionary<string, string> { { "W", "600" }, { "H", "500" } });
SendPacket("SetTitle", new Dictionary<string, string> { { "Text", "<color=#FF0000>Server Admin Panel</color>" } });

// 2. Register assets (images and audio)
SendPacket("RegisterImage", new Dictionary<string, string> {
    { "Name", "server_logo.png" },
    { "Url", "https://yourdomain.com/images/logo.png" }
});

SendPacket("RegisterSound", new Dictionary<string, string> {
    { "Name", "notification_sound" },
    { "Url", "https://yourdomain.com/sounds/notification.mp3" }
});

// 3. Create tabs
SendPacket("CreateTab", new Dictionary<string, string> { { "Name", "Home" } });

// 4. Add content to Home tab
SendPacket("AddImage", new Dictionary<string, string> {
    { "Img", "server_logo.png" },
    { "X", "250" }, { "Y", "20" },
    { "W", "100" }, { "H", "100" }
});

SendPacket("AddText", new Dictionary<string, string> {
    { "Text", "Welcome to the admin panel!" },
    { "X", "20" }, { "Y", "150" }
});

SendPacket("AddButton", new Dictionary<string, string> {
    { "Text", "Heal All Players" },
    { "Cmd", ".healall" },
    { "X", "20" }, { "Y", "200" },
    { "W", "560" }, { "H", "40" }
});

// 5. Create Settings tab
SendPacket("CreateTab", new Dictionary<string, string> { { "Name", "Settings" } });

SendPacket("AddButton", new Dictionary<string, string> {
    { "Text", "Play Notification" },
    { "Cmd", ".zuiplay notification_sound 1.0" },
    { "X", "20" }, { "Y", "50" },
    { "W", "200" }, { "H", "30" }
});

// 6. Force open the window
SendPacket("Open", new Dictionary<string, string>());
```

#### Available Server Packets

| Packet Type | Data Fields | Description |
|-------------|-------------|-------------|
| `SetPlugin` | `Plugin` | Sets the plugin context |
| `SetTargetWindow` | `Window` | Sets the target window |
| `SetUICustom` | `W`, `H` | Creates custom window with dimensions |
| `SetTitle` | `Text` | Sets window title |
| `CreateTab` | `Name` | Creates a new tab |
| `AddCategory` | `Name`, `X`, `Y` | Adds category label |
| `AddText` | `Text`, `X`, `Y` | Adds text element |
| `AddButton` | `Text`, `Cmd`, `X`, `Y`, `W`, `H` | Adds button |
| `AddImage` | `Img`, `X`, `Y`, `W`, `H` | Adds image |
| `RegisterImage` | `Name`, `Url` | Registers image from URL |
| `RegisterSound` | `Name`, `Url` | Registers audio from URL |
| `Open` | *(none)* | Forces window to open |

---

## 🎨 Custom UI Design Tool

**Want to design your custom UI visually instead of coding coordinates?**

Use the official ZUI Canvas Designer at **[https://zanakinz.github.io/ZUI](https://zanakinz.github.io/ZUI)**

This interactive tool allows you to:
- ✨ Visually position UI elements (buttons, text, images, categories)
- 👁️ Preview your layout in real-time
- 📤 Export code directly for your mod
- 🎯 Experiment with different window sizes and arrangements
- 🎨 Test color schemes and layouts instantly

Instead of manually calculating X/Y coordinates, use the designer to drag and drop elements, then copy the generated code into your mod!

---

## 📐 Positioning System

Coordinates are in pixels from the top-left corner (0, 0):
- **X increases** going right
- **Y increases** going down
- Origin is the top-left corner of the window

Example: Position (20, 50) is 20 pixels from the left edge and 50 pixels from the top.

---

## 🎨 Color Support

ZUI supports Unity Rich Text color tags for all text elements:

```csharp
"<color=#FF0000>Red Text</color>"
"<color=#00FF00>Green Text</color>"
"<color=#3498DB>Blue Text</color>"
"<color=#E74C3C>Bright Red</color>"
"<color=#2ECC71>Green Success</color>"
"<color=#F39C12>Orange Warning</color>"
"<color=#9B59B6>Purple</color>"
"<color=#ECF0F1>Light Gray</color>"
```

You can also use named colors:
```csharp
"<color=red>Red Text</color>"
"<color=green>Green Text</color>"
"<color=blue>Blue Text</color>"
```

---

## 🔧 Integration Guide for Mod Developers

ZUI supports two integration approaches depending on whether your mod is client-side or server-side.

### Client-Side Mods: Soft Dependency Pattern (Recommended)

To integrate ZUI without requiring it at compile-time, use reflection:

```csharp
using BepInEx;
using BepInEx.Unity.IL2CPP;
using System;
using System.Linq;
using System.Reflection;

[BepInPlugin("com.yourname.yourmod", "Your Mod", "1.0.0")]
[BepInDependency("Zanakinz.ZUI", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BasePlugin
{
    private static Type _zui;

    public override void Load()
    {
        if (InitZUI())
        {
            RegisterUI();
        }
    }

    private bool InitZUI()
    {
        if (!IL2CPPChainloader.Instance.Plugins.ContainsKey("Zanakinz.ZUI")) 
            return false;
            
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "ZUI");
        _zui = assembly?.GetType("ZUI.API.ZUI");
        return _zui != null;
    }

    private void Call(string name, params object[] args)
    {
        if (_zui == null) return;
        var method = _zui.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == name && m.GetParameters().Length == args.Length);
        method?.Invoke(null, args);
    }

    private void RegisterUI()
    {
        Call("SetPlugin", "YourMod");
        Call("SetTargetWindow", "Main");
        Call("AddCategory", "Features");
        Call("AddButton", "Test", ".test");
    }
}
```

### Server-Side Mods: Packet-Based Integration

Server mods don't need any dependency on ZUI.dll. Simply send formatted chat messages:

```csharp
using System.Collections.Generic;
using System.Text.Json;

public class ServerMod
{
    private void SendPacket(string type, Dictionary<string, string> data)
    {
        var packet = new
        {
            Type = type,
            Plugin = "MyServerMod",
            Window = "ServerWindow",
            Data = data
        };

        string json = JsonSerializer.Serialize(packet);
        string message = "[[ZUI]]" + json;
        
        // Send via your server's chat system
        ServerChatUtils.SendSystemMessageToUser(userEntity, message);
    }
    
    private void CreateServerUI()
    {
        SendPacket("SetPlugin", new Dictionary<string, string> { { "Plugin", "MyServerMod" } });
        SendPacket("SetTargetWindow", new Dictionary<string, string> { { "Window", "MyPanel" } });
        SendPacket("SetUICustom", new Dictionary<string, string> { { "W", "500" }, { "H", "300" } });
        SendPacket("SetTitle", new Dictionary<string, string> { { "Text", "Server Panel" } });
        SendPacket("AddButton", new Dictionary<string, string> {
            { "Text", "Click Me" },
            { "Cmd", ".mycommand" },
            { "X", "20" }, { "Y", "100" },
            { "W", "200" }, { "H", "40" }
        });
        SendPacket("Open", new Dictionary<string, string>());
    }
}
```

**Key Benefits:**
- No client mod required (players only need ZUI)
- Server has full control over UI
- Can stream images and audio from web URLs
- Perfect for admin tools and server-specific features

---

## 🖼️ Working with Images

**Client-Side:** Place your custom PNG/JPG/GIF images in:
```
BepInEx/plugins/Sprites/
```

Then reference them by filename in your code:
```csharp
ZUI.AddImage("your_image.png", x, y, width, height);
ZUI.AddImage("animated.gif", x, y, width, height); // GIFs play automatically
```

**Server-Side:** Register images from web URLs:
```csharp
// Server sends this packet
SendPacket("RegisterImage", new Dictionary<string, string> {
    { "Name", "logo.png" },
    { "Url", "https://yourdomain.com/logo.png" }
});

// Then use it in UI
SendPacket("AddImage", new Dictionary<string, string> {
    { "Img", "logo.png" },
    { "X", "20" }, { "Y", "20" },
    { "W", "100" }, { "H", "100" }
});
```

**Note:** Images are downloaded asynchronously and cached in memory on the client.

---

## ⚙️ Configuration

ZUI's configuration file is located at: `BepInEx/config/Zanakinz.ZUI.cfg`

### Important Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `AllowServerAudioDownloads` | `false` | **🚨 SECURITY**: Allow servers to download audio files. Only enable on trusted servers! |
| `CommandDelayInMilliseconds` | `50` | Delay between command executions (0-5000ms). 50ms recommended for responsive UI. |
| `ClearServerMessages` | `true` | Automatically clear server/command messages from chat |
| `UITransparency` | `0.6` | Panel transparency (0.0 = invisible, 1.0 = opaque) |
| `UseHorizontalContentLayout` | `true` | Horizontal vs vertical layout for main content panel |
| `ServerHasBloodCraft` | `false` | Enable if server has BloodCraft mod |
| `ServerHasKindredCommands` | `true` | Enable if server has KindredCommands mod |
| `ServerHasKinPonds` | `true` | Enable if server has KinPonds mod |
| `ServerHasScarletSigns` | `true` | Enable if server has ScarletSigns mod |

### 🔐 Security Configuration Reminder

The `AllowServerAudioDownloads` setting is **disabled by default** to protect users from potentially malicious audio downloads. Only enable this if:
- You completely trust the server administrator
- You know the server uses audio features responsibly
- You understand the risks of downloading external files

---

## 🚧 Experimental Features (Work in Progress)

The following features exist in the API but are **not yet functional**. They are included for future development:

- `AddInput()` - Text input fields
- `AddToggle()` - Checkbox toggles  
- `AddRadio()` - Radio button groups
- `AddSlider()` - Numeric sliders
- `AddDropdown()` - Dropdown lists

These methods are present in the codebase but do not currently affect the UI. **Do not use them in production mods** until they are officially documented as ready.

---

## 📝 Complete Example

```csharp
// Setup plugin with multiple categories, tabs, and custom window
ZUI.SetPlugin("AdvancedMod");
ZUI.SetTargetWindow("AdminPanel");
ZUI.SetUI(700, 500);

ZUI.SetTitle("<color=#E74C3C>Admin Control Panel</color>");

// === TAB 1: Player Management ===
ZUI.CreateTab("Players");

ZUI.AddCategory("<color=#3498DB>Player Management</color>", 20f, 50f);
ZUI.AddButton("Full Heal", ".heal max", 20f, 90f, 300f, 30f);
ZUI.AddButton("Speed Boost", ".buff speed", 20f, 130f, 300f, 30f);
ZUI.AddButton("God Mode Toggle", ".god", 20f, 170f, 300f, 30f);

// === TAB 2: Server Tools ===
ZUI.CreateTab("Server");

ZUI.AddCategory("<color=#2ECC71>Server Tools</color>", 20f, 50f);
ZUI.AddButton("Spawn Items", ".spawn all", 20f, 90f, 300f, 30f);
ZUI.AddButton("Teleport", ".tp", 20f, 130f, 300f, 30f);
ZUI.AddButton("Clear Area", ".clear", 20f, 170f, 300f, 30f);

// === TAB 3: Media ===
ZUI.CreateTab("Media");

// Register and play audio
ZUI.RegisterSound("admin_alert", "https://yourdomain.com/alert.mp3");
ZUI.AddButton("Play Alert", ".zuiplay admin_alert 0.7", 20f, 50f, 200f, 30f);

// Display animated GIF
ZUI.AddImage("server_status.gif", 250f, 100f, 200f, 150f);

// Status Display
ZUI.AddText("Status: <color=#2ECC71>Ready</color>", 20f, 450f);

// Listen for UI updates
ZUI.OnButtonsChanged += () => {
    Console.WriteLine("UI buttons updated!");
};
```

---

## 📁 Troubleshooting

**UI doesn't appear:**
- Verify ZUI.dll and Sprites folder are in `BepInEx/plugins/`
- Check BepInEx console for errors
- Ensure ZUI is loaded before your mod (check load order)

**Custom window not showing:**
- Verify `SetUI(width, height)` is called before adding elements
- Check window dimensions are reasonable (100-2000 pixels)
- Ensure `SetTargetWindow()` uses a unique name

**Images not displaying:**
- Verify image files are in `BepInEx/plugins/Sprites/` folder
- Check filename matches exactly (case-sensitive)
- Supported formats: PNG, JPG, GIF
- For web images: Ensure URL is accessible and uses HTTPS

**Audio not playing:**
- Check `AllowServerAudioDownloads` is enabled if using server audio
- Verify audio files are in `BepInEx/plugins/Audio/` folder for local files
- Supported formats: WAV, MP3, OGG
- Check BepInEx console for download errors
- Ensure URLs are accessible and use HTTPS

**Buttons not responding:**
- Verify commands start with a period (e.g., ".heal")
- Check that the command handler is registered in your mod
- Look for errors in BepInEx console

**Tabs not appearing:**
- Tabs only work in Custom UI windows created with `SetUI()`
- Tabs cannot be used in the legacy Main menu
- Ensure `CreateTab()` is called after `SetUI()`

**Integration not working:**
- Client-side: Ensure BepInDependency is set correctly
- Client-side: Check that InitZUI() returns true
- Client-side: Verify reflection calls match ZUI API method signatures
- Server-side: Verify JSON packet format is correct
- Server-side: Ensure [[ZUI]] prefix is at the start of the message
- Server-side: Check that players have ZUI installed

---

## 📦 Built-in Integrations

ZUI includes native support for:
- **BloodCraft** - Extensive RPG Systems
- **KinPonds** - Turn wells into ponds!
- **ScarletSigns** - Comprehensive Floating Text/Sign tool
- **KindredCommands** - The ULTIMATE Admin toolkit

These integrations can be enabled/disabled in `ZUI.cfg`.

---

## 🙏 Credits

- **Panthernet** - BloodCraftUI - OnlyFams was used as a base template
- **Zanakinz** - ZUI development and evolution
- **The VRising modding community** - For all the references and support

---

## 🤖 AI Disclosure

- AI was used in creation of readme's
- AI was used for debugging
- AI was used for image asset generation as I am not an artist

---

## 📄 License

This product is licensed under LGPLv3, but the base plugin 'BloodCraftUI - OnlyFams' is registered under MIT, so therefore this is a dual-license between MIT and my works which is LGPLv3.

See [LICENSE.LGPLv3](LICENSE.LGPLv3) for LGPLv3 details and [LICENSE.mit](LICENSE.mit) for MIT details.
