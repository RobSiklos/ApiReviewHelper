# ApiReviewHelper
A tool to help see the public API of a .NET assembly, and show the differences in the that API since the last time the tool was run.

## How To Use The Tool To See Your Public API

1. Download the release and extract it to a temporary directory.
1. Create a working folder. I call mine `apidiff`
1. In your working folder, create a file called `AssemblyList.txt`. This file should contain the paths to all the assemblies you want to analyze (one per line). For example:
1. Open a shell (`cmd` or PowerShell) and change to your working folder.
1. Create an HTML baseline using the following syntax: `ApiReviewHelper.exe create-baseline .\AssemblyList.txt .\baseline.html HTML` (you will probably need to specify the full path to the EXE).
1. You can now view `baseline.html`, which will show the entire public API of your .NET assembly.

## How To See The Changes In Your API
1. Create an initial baseline in XML format using syntax like the following: `ApiReviewHelper.exe create-baseline .\AssemblyList.txt .\baseline-original.xml XML`
1. Make whatever changes to your code you want.
1. Create a new baseline to compare using a command like `ApiReviewHelper.exe create-baseline .\AssemblyList.txt .\baseline-new.xml XML`
1. Create an HTML file showing the diff using a command like `ApiReviewHelper.exe create-diff .\baseline-original.xml .\baseline-new.xml .\diff.html`
1. Open `diff.html` in a web browser to see the differences.

### Example AssemblyList.txt
```
c:\src\MyProject\bin\Debug\MyProject.dll
c:\src\MyProject\bin\Debug\MyLibrary.dll
```
