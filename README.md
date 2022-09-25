# PiPanel
RaspberryPi example data display program with low-level LCD drivers written in .NET Core

This project is a Visual Studio 2022 solution.

## Supported displays

  * ILI9327 TFT color screen
  * PCD8544 (known as Nokia LCD) monochrome LCD

## Used libraries for this example

None that only WiringPi is necessary if you want to borrow LCD driver code.

  * WiringPi (with C# wrapper originally created by Daniel J Riches)
  * NLog for logging and tracing communication
  * RunProcessAsTask for async temperature measuring
  * Newtonsoft.Json for Covid data parsing
  
