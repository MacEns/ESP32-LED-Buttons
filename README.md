# ESP32 & Raspberry Pi Pico Buttons (.NET nanoFramework)

C#/.NET programs for an ESP32 dev board and a Raspberry Pi Pico (RP2040):
five momentary push buttons, each toggling its own LED on press. Both projects
use the same event-driven behavior and run on
[.NET nanoFramework](https://www.nanoframework.net/).

## Projects

- `Esp32Buttons` — original ESP32 variant.
- `PicoButtons` — Raspberry Pi Pico variant with Pico-specific logical GPIO
  assignments.

## Wiring

### ESP32

| Button | Button GPIO | LED | LED GPIO |
|--------|-------------|-----|----------|
| 1      | 15          | 1   | 16       |
| 2      | 4           | 2   | 17       |
| 3      | 5           | 3   | 19       |
| 4      | 18          | 4   | 21       |
| 5      | 23          | 5   | 22       |

### Raspberry Pi Pico

| Button | Button GPIO | Pico header pin | LED | LED GPIO | Pico header pin |
|--------|-------------|-----------------|-----|----------|-----------------|
| 1      | 2           | 4               | 1   | 7        | 10              |
| 2      | 3           | 5               | 2   | 8        | 11              |
| 3      | 4           | 6               | 3   | 9        | 12              |
| 4      | 5           | 7               | 4   | 10       | 14              |
| 5      | 6           | 9               | 5   | 11       | 15              |

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

- **Min** = keeps the GPIO within the ESP32's conservative ~12 mA load used by
  this guide
  (red/yellow/green: 120 Ω ≈ 11 mA). Below this risks stressing the pin.
- **Max** = stays bright enough to see clearly (≈2 mA). Larger values still
  light the LED, just progressively dimmer.
- Blue/white forward voltage nearly equals the 3.3 V supply, so current
  self-limits to a few mA; the resistor mostly guards against overdrive at the
  min end, and values above ~220 Ω leave them too dim.
- A single common value of 150–200 Ω works for all five colours.

The Pico values are RP2040 logical GPIO numbers, not physical header numbers.
To use different pins, edit `ButtonPins` and `LedPins` in the selected project's
`Program.cs`.

## Prerequisites

1. Install .NET 10, Visual Studio 2022 Build Tools with the .NET desktop build
   workload, or the corresponding supported mono build tooling.
2. Install `nanoff` and flash the appropriate firmware:

   ```powershell
   dotnet tool install -g nanoff

   # ESP32 connected on COM3
   nanoff --target ESP32_REV0 --serialport COM3 --update

   # Raspberry Pi Pico / Pico W: first hold BOOTSEL while reconnecting USB
   nanoff --platform rpi_pico --target PICO_RP2040 --update
   # For Pico W use target PICO_RP2040_W
   ```

   Replace `COM3` with the ESP32 port. Pico firmware is copied using the
   board's BOOTSEL UF2 drive, so no serial port is needed. For ESP32-S3/C3
   boards use the matching target or auto-detect with
   `nanoff --platform esp32 --serialport COM3 --update`.

3. Install either:
   - **Visual Studio 2022** + the ".NET nanoFramework Extension" (from the VS Marketplace), or
   - **VS Code** + the "nanoFramework" extension.

## Build & deploy

### Command line

Build the whole solution with the nanoFramework project system:

```powershell
nuget restore .\Esp32Buttons.sln
msbuild .\Esp32Buttons.sln /p:platform="Any CPU" /p:Configuration=Release /verbosity:minimal
```

The Pico deployment image is generated at
`PicoButtons\bin\Release\PicoButtons.bin` and the ESP32 image at
`Esp32Buttons\bin\Release\Esp32Buttons.bin`.

### VS Code

1. Open this folder.
2. Run **nanoFramework: Build Project**, selecting the desired project.
3. Run **nanoFramework: Deploy Project** and select the connected
   nanoFramework device. A normal nanoFramework Pico connection is used for
   app deployment after the firmware has been flashed once.

For UF2 application deployment, the extension/`nanoff` can alternatively be
used in Pico BOOTSEL mode:

```powershell
nanoff --platform rpi_pico --target PICO_RP2040 --uf2deploy --image .\PicoButtons\bin\Release\PicoButtons.bin
```

### Visual Studio

1. Open `Esp32Buttons.sln`. NuGet packages restore automatically.
2. In **Device Explorer**, select the connected ESP32 or Pico.
3. Right-click `Esp32Buttons` or `PicoButtons` → **Deploy**. Press F5 to debug.

## Behavior

- Each button press toggles its corresponding LED.
- After an accepted press, that button is locked out for 250 ms, filtering
  contact bounce on both press and release.
- Press events are logged to the debugger output (visible when debugging from
  Visual Studio).
