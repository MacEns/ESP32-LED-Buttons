# ESP32 Buttons (.NET nanoFramework)

A C#/.NET program for an ESP32 dev board: five momentary push buttons, each
toggling its own LED on press. Built on [.NET nanoFramework](https://www.nanoframework.net/),
which runs a trimmed-down .NET CLR directly on the ESP32.

## Wiring

| Button | Button GPIO | LED | LED GPIO |
|--------|-------------|-----|----------|
| 1      | 15          | 1   | 16       |
| 2      | 4           | 2   | 17       |
| 3      | 5           | 3   | 19       |
| 4      | 18          | 4   | 21       |
| 5      | 23          | 5   | 22       |

- **Buttons**: wire each button between its GPIO and **GND**. The internal
  pull-up is enabled, so a press reads LOW (falling edge).
- **LEDs**: wire each LED anode (long leg) to its GPIO through a series
  resistor (see table below), cathode to GND. HIGH = on.

### LED series resistors

Driven from a 3.3 V GPIO (recommended max ~12 mA continuous per pin):

| LED colour | Forward voltage | Min Ω | Max Ω | Recommended |
|------------|-----------------|-------|-------|-------------|
| Red        | ~2.0 V          | 120 Ω | 680 Ω | 150–220 Ω   |
| Yellow     | ~2.0 V          | 120 Ω | 680 Ω | 150–220 Ω   |
| Green      | ~2.0 V          | 120 Ω | 680 Ω | 150–220 Ω   |
| Blue       | ~3.3 V          | 47 Ω  | 220 Ω | 100 Ω       |
| White      | ~3.3 V          | 47 Ω  | 220 Ω | 100 Ω       |

- **Min** = keeps the GPIO within its safe ~12 mA continuous load
  (red/yellow/green: 120 Ω ≈ 11 mA). Below this risks stressing the pin.
- **Max** = stays bright enough to see clearly (≈2 mA). Larger values still
  light the LED, just progressively dimmer.
- Blue/white forward voltage nearly equals the 3.3 V supply, so current
  self-limits to a few mA; the resistor mostly guards against overdrive at the
  min end, and values above ~220 Ω leave them too dim.
- A single common value of 150–200 Ω works for all five colours.

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
- After an accepted press, that button is locked out for 300 ms, filtering
  contact bounce on both press and release.
- Press events are logged to the debugger output (visible when debugging from
  Visual Studio).
