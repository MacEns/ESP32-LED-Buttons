# ESP32 Buttons (.NET nanoFramework)

A C#/.NET program for an ESP32 dev board: four momentary push buttons, each
toggling its own LED on press. Built on [.NET nanoFramework](https://www.nanoframework.net/),
which runs a trimmed-down .NET CLR directly on the ESP32.

## Wiring

| Button | Button GPIO | LED | LED GPIO |
|--------|-------------|-----|----------|
| 1      | 15          | 1   | 16       |
| 2      | 4           | 2   | 17       |
| 3      | 5           | 3   | 19       |
| 4      | 18          | 4   | 21       |

- **Buttons**: wire each button between its GPIO and **GND**. The internal
  pull-up is enabled, so a press reads LOW (falling edge).
- **LEDs**: wire each LED anode to its GPIO through a ~330 Ω resistor, cathode
  to GND. HIGH = on.

To use different pins, edit the `ButtonPins` and `LedPins` arrays in
[Program.cs](./Esp32Buttons/Program.cs).

## Prerequisites

1. **Flash the nanoFramework firmware** (one-time, replaces any Arduino/IDF app):

   ```powershell
   dotnet tool install -g nanoff
   nanoff --target ESP32_REV0 --serialport COM3 --update
   ```

   Replace `COM3` with your board's port. For ESP32-S3/C3 boards use the
   matching target (e.g. `ESP32_S3`, `ESP32_C3`), or let `nanoff --platform esp32 --serialport COM3 --update` auto-detect.

2. **Install the IDE tooling** (either one):
   - **Visual Studio 2022** + the ".NET nanoFramework Extension" (from the VS Marketplace), or
   - **VS Code** + the "nanoFramework" extension.

## Build & deploy

### Visual Studio
1. Open `Esp32Buttons.sln`. NuGet packages restore automatically.
2. In the **Device Explorer** window, select your connected ESP32.
3. Right-click the project → **Deploy**. Press F5 to debug on the device.

### VS Code
1. Open this folder.
2. Run the command **nanoFramework: Build Project**, then
   **nanoFramework: Deploy Project** and pick the serial port.

## Behavior

- Each button press toggles its corresponding LED.
- A 50 ms software debounce filters switch bounce.
- Press events are logged to the debugger output (visible when debugging from
  Visual Studio).
