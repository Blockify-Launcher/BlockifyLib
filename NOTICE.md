# Third-party notices — BlockifyLib

BlockifyLib is a launch engine for Minecraft: Java Edition. Parts of it are adapted
from the following open-source projects. Their original licenses apply to those parts.

## CmlLib.Core

- Source: https://github.com/CmlLib/CmlLib.Core
- Copyright (c) AlphaBs and CmlLib.Core contributors
- License: MIT
- Used for: version/library/asset model and checkers, launch argument building,
  Microsoft login handler (`Launcher/Microsoft/*`, `Launcher/src/*`), MSAL cache settings.

## XboxAuthNet.Game (part of CmlLib)

- Source: https://github.com/CmlLib/CmlLib.Core.Auth.Microsoft
- Copyright (c) AlphaBs and contributors
- License: MIT
- Used for: `Launcher/XboxAuthNet.Game/**` (Xbox / Microsoft account authentication flow).

## MIT License text (applies to the projects above)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## NuGet packages

| Package | License |
|---|---|
| LZMA-SDK | Public domain |
| Microsoft.Identity.Client, Microsoft.Identity.Client.Extensions.Msal | MIT (Microsoft) |
| Microsoft.Extensions.Logging.Abstractions, System.Text.Json, NETStandard.Library | MIT (.NET Foundation) |
| Newtonsoft.Json | MIT (James Newton-King) |
| SharpZipLib | MIT (ICSharpCode) |
| XboxAuthNet | MIT (AlphaBs) |
| ConfigureAwait.Fody | MIT (Fody contributors) |

Minecraft is a trademark of Mojang AB / Microsoft. BlockifyLib is not affiliated with or endorsed by them.
