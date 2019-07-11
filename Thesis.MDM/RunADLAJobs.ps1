#Registering assemblies to the Data Lake Analytics

Param(
    [string] [Parameter(Mandatory=$true)] $DataLakeStoreName,
    [string] [Parameter(Mandatory=$true)] $DataLakeAnalyticsName,
	[switch] [Parameter(Mandatory=$true)] $Runjobs 
)

Login-AzureRmAccount

$ScriptDirectory = [System.IO.Path]::GetDirectoryName($PSScriptRoot)
$ProjectPath = (get-item $ScriptDirectory).Parent.Parent.Parent.Parent.FullName

$RelativeNewtonsoftPath = 'DataLakeAnalytics.ClassLibrary\bin\Debug\Newtonsoft.Json.dll'
$NewtonsoftAssemblyPath = "$ProjectPath\$RelativeNewtonsoftPath"
$RelativeClassLibraryPath = 'DataLakeAnalytics.ClassLibrary\bin\Debug\DataLakeAnalytics.ClassLibrary.dll'
$ClassLibraryAssemblyPath = "$ProjectPath\$RelativeClassLibraryPath"

#U-SQL scripts path
$RelativeRegisterAssemblyPath = 'Thesis.MDM.DataLakeAnalytics\RegisterAssemblies.usql'
$UsqlRegisterAssemblyPath = "$ProjectPath\$RelativeRegisterAssemblyPath"
$RelativeCleanDataPath = 'Thesis.MDM.DataLakeAnalytics\CleanData.usql'
$UsqlCleanDataPath = "$ProjectPath\$RelativeCleanDataPath"
    
New-AzureRmDataLakeStoreItem -AccountName $DataLakeStoreName -Path '/Assemblies' -Folder -Force

Import-AzureRmDataLakeStoreItem -AccountName $DataLakeStoreName -Path $ClassLibraryAssemblyPath -Destination '/Assemblies/DataLakeAnalytics.ClassLibrary.dll' -Force
Import-AzureRmDataLakeStoreItem -AccountName $DataLakeStoreName -Path $NewtonsoftAssemblyPath -Destination '/Assemblies/Newtonsoft.Json.dll' -Force

Write-Output 'Registering assemblies...' 

$job = Submit-AdlJob -AccountName $DataLakeAnalyticsName –ScriptPath $UsqlRegisterAssemblyPath -Name "RegisterAssemblies"

Wait-AdlJob -Account $DataLakeAnalyticsName -JobId $job.JobId

Write-Output 'Assemblies has been registered successfully'	

if($Runjobs){
	Write-Output 'Running "CleanData.usql"...' 

	$job = Submit-AdlJob -AccountName $DataLakeAnalyticsName –ScriptPath $UsqlCleanDataPath -Name "CleanData"

	Wait-AdlJob -Account $DataLakeAnalyticsName -JobId $job.JobId

	Write-Output 'Job has been completed.'	
}

Read-Host -Prompt "Press Enter to exit"