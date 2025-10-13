# .NET 9.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that an .NET 9.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 9.0 upgrade.
3. Upgrade WebBoard.API\WebBoard.API.csproj
4. Upgrade WebBoard.Tests\WebBoard.Tests.csproj

## Settings

This section contains settings and data used by execution steps.

### Excluded projects

| Project name                                   | Description                 |
|:-----------------------------------------------|:---------------------------:|

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                        | Current Version | New Version | Description                                   |
|:------------------------------------|:---------------:|:-----------:|:----------------------------------------------|
| Microsoft.AspNetCore.OpenApi        |   8.0.11        |  9.0.9      | Replace with new package for .NET 9.0         |
| Microsoft.AspNetCore.SignalR        |   1.2.0         |             | Replace with Microsoft.AspNetCore.SignalR.Client 9.0.9 |
| Microsoft.AspNetCore.SignalR.Client |                 |  9.0.9      | Replacement for Microsoft.AspNetCore.SignalR   |
| Microsoft.VisualStudio.Web.CodeGeneration.Design | 8.0.7 | 9.0.0 | Replace with new package for .NET 9.0         |

### Project upgrade details

#### WebBoard.API\WebBoard.API.csproj modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net9.0`

NuGet packages changes:
  - Microsoft.AspNetCore.OpenApi should be updated from `8.0.11` to `9.0.9`
  - Microsoft.AspNetCore.SignalR should be replaced with Microsoft.AspNetCore.SignalR.Client `9.0.9`
  - Microsoft.VisualStudio.Web.CodeGeneration.Design should be updated from `8.0.7` to `9.0.0`

#### WebBoard.Tests\WebBoard.Tests.csproj modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net9.0`

NuGet packages changes:
  - No NuGet package changes required.
