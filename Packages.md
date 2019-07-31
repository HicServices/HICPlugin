

# Packages Used

### Risk Assessment common to all:
1. Packages on NuGet are virus scanned by the NuGet site.
2. This package is widely used and is actively maintained.
3. It is open source (Except HIC only pacakges).

| Package | Source Code | Version | License | Purpose | Additional Risk Assessment |
| ------- | ------------| --------| ------- | ------- | -------------------------- |
| [Nunit](https://nunit.org/) |[GitHub](https://github.com/nunit/nunit) | [3.11.0](https://www.nuget.org/packages/NUnit/3.11.0) | [MIT](https://opensource.org/licenses/MIT) | Unit testing |
| NUnit3TestAdapter | [GitHub](https://github.com/nunit/nunit3-vs-adapter)| [3.13.0](https://www.nuget.org/packages/NUnit3TestAdapter/3.13.0) | [MIT](https://opensource.org/licenses/MIT) | Run unit tests from within Visual Studio |
| HIC.RDMP.Plugin | [GitHub](https://github.com/HicServices/RDMP) | [3.1.0](https://www.nuget.org/packages/HIC.RDMP.Plugin/3.1.0) | [GPL 3.0](https://www.gnu.org/licenses/gpl-3.0.html) | Interact with RDMP objects, base classes for plugin components etc | |
| HIC.RDMP.Plugin.UI | [GitHub](https://github.com/HicServices/RDMP) | [3.1.0](https://www.nuget.org/packages/HIC.RDMP.Plugin.UI/3.1.0) |[GPL 3.0](https://www.gnu.org/licenses/gpl-3.0.html) | Interact with RDMP user interface API layer | |
| SharpCompress | [GitHub](https://github.com/adamhathcock/sharpcompress) | [0.23.0](https://www.nuget.org/packages/SharpCompress/0.23.0) | [MIT](https://opensource.org/licenses/MIT) | Handles diverse zip file formats e.g. tar/rar | |
| SharpZipLib | [GitHub](https://github.com/icsharpcode/SharpZipLib) | [1.1.0](https://www.nuget.org/packages/SharpZipLib/1.1.0) | [MIT](https://opensource.org/licenses/MIT) | Handles diverse zip file formats e.g. tar/rar | We have two different zip file packages as dependencies due to legacy merging of multiple seperate repos|
| System.Drawing.Common | [GitHub](https://github.com/dotnet/corefx)  | [4.5.1](https://www.nuget.org/packages/System.Drawing.Common/4.5.1) |[MIT](https://opensource.org/licenses/MIT) | Enables working with Bitmap resources |  |
| System.Data.SqlClient | [GitHub](https://github.com/dotnet/corefx) | [4.6.1](https://www.nuget.org/packages/System.Data.SqlClient/4.6.1) | [MIT](https://opensource.org/licenses/MIT) | Enables interaction with Microsoft Sql Server databases |  |
| System.ServiceModel.Http | [GitHub](https://github.com/dotnet/corefx)  | [4.5.3](https://www.nuget.org/packages/System.ServiceModel.Http/4.5.3) | [MIT](https://opensource.org/licenses/MIT) | Enables interaction with Web APIs (SciStore) |  |
| [NLog](https://nlog-project.org/) | [GitHub](https://github.com/NLog/NLog) | [4.6.3](https://www.nuget.org/packages/NLog/4.6.3) | [BSD 3-Clause](https://github.com/NLog/NLog/blob/dev/LICENSE.txt) | Flexible user configurable logging | |
| HIC.Demography | Closed Source | 1.0.0 <!--1.0.0 in https://www.nuget.org/api/v2/-->) | N\A | Models for interfacing with the CHI queuing service in HIC | This is an internally developed HIC package|
| InterfaceToJira | Closed Source | 1.1.5098 <!--1.1.5098 in https://www.nuget.org/api/v2/-->) | N\A| For interacting with the HIC local Jira server | This is an internally developed HIC package|