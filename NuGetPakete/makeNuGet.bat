set V="1.2.4"

cls

del *.nupkg

cd ..\ECoSConnector
..\3rdparty\nuget.exe pack ECoSConnector.nuspec -Version "%V%" -Verbosity Detailed -OutputDirectory "..\NuGetPakete"
cd ..\NuGetPakete

cd ..\ECoSCore
..\3rdparty\nuget.exe pack ECoSCore.nuspec -Version "%V%" -Verbosity Detailed -OutputDirectory "..\NuGetPakete"
cd ..\NuGetPakete

cd ..\ECoSEntities
..\3rdparty\nuget.exe pack ECoSEntities.nuspec -Version "%V%" -Verbosity Detailed -OutputDirectory "..\NuGetPakete"
cd ..\NuGetPakete

cd ..\ECoSListener
..\3rdparty\nuget.exe pack ECoSListener.nuspec -Version "%V%" -Verbosity Detailed -OutputDirectory "..\NuGetPakete"
cd ..\NuGetPakete

cd ..\ECoSToolsLibrary
..\3rdparty\nuget.exe pack ECoSToolsLibrary.nuspec -Version "%V%" -Verbosity Detailed -OutputDirectory "..\NuGetPakete"
cd ..\NuGetPakete

cd ..\ECoSUtils
..\3rdparty\nuget.exe pack ECoSUtils.nuspec -Version "%V%" -Verbosity Detailed -OutputDirectory "..\NuGetPakete"
cd ..\NuGetPakete
