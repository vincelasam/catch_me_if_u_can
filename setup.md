**Godot 2D Strategy Game — Teammate Setup Guide
Prerequisites
Make sure your system is running Windows 10 or 11 (64-bit). All tools below are free.**

**1. Visual Studio
**Download Visual Studio from visualstudio.microsoft.com. During installation, check the .NET desktop development workload — this is required for C# scripting.
If you already have Visual Studio installed, open the Visual Studio Installer, click Modify, and make sure the .NET desktop development workload is checked. Add it if it isn't.

**2. .NET SDK
**Go to dotnet.microsoft.com/download and download the latest .NET 8 SDK (64-bit Windows). Run the installer and follow the default steps.
Once installed, verify it worked by opening Command Prompt and running:
dotnet --version
It should print a version number like 8.0.x. If it says "not recognized", restart your computer and try again. If it still fails, reinstall the SDK.

**3. Godot Engine (.NET Version)
**Go to godotengine.org/download and download the .NET version for Windows 64-bit. There are two versions on the page — Standard and .NET. You must pick .NET for C# support.
The download is a ZIP file. Extract it anywhere you like, for example C:\Godot\. There is no installer — Godot runs directly as an .exe. The executable will be named something like:
Godot_v4.x.x-stable_mono_win64.exe
When you run it for the first time, Windows may show a blue SmartScreen warning. This is normal. Click More info, then Run anyway.

**4. Clone the Repository
**Open Git Bash or any terminal, navigate to the folder where you want to store the project, and run:
bashgit clone https://github.com/YOUR_USERNAME/YOUR_REPO_NAME.git
Ask your team lead for the exact repository URL if you don't have it.

**5. Open the Project in Godot
**
Launch the Godot .exe.
In the Project Manager, click Import.
Navigate to the cloned project folder and select the project.godot file.
Click Import & Edit. The project will open in the editor.


6. Connect Visual Studio to Godot
This makes Godot automatically open Visual Studio whenever you click on a C# script.

Inside the Godot editor, go to Editor → Editor Settings.
In the search bar, type dotnet.
Navigate to Dotnet → Editor.
Set External Editor to Visual Studio.
Click Save.

From now on, double-clicking any .cs script inside Godot will open it directly in Visual Studio with the cursor on the correct line.

**7. Open the Solution in Visual Studio
**Inside the cloned project folder, find and open the .sln file in Visual Studio. This gives you full IntelliSense, code completion, and debugging support for all C# scripts in the project.
You only need to do this once — after that, Visual Studio will remember the solution.

**8. Build Workflow
**Every time you write or modify C# code, build before running in Godot:

In Godot, click the hammer icon (🔨) in the top-right, OR
In Visual Studio, press Ctrl + Shift + B

If there are build errors, Godot will not run your scripts. Always check the build output for errors before pressing Play.
Your day-to-day workflow will be:
Write code in Visual Studio → Build → Test in Godot
