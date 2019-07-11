#Azure function deployment (uploading files to the fileshare)
Param(
    [string] [Parameter(Mandatory=$true)] $StorageAccountName,
    [string] [Parameter(Mandatory=$true)] $StorageAccountKey,
    [string] [Parameter(Mandatory=$true)] $FileShareName
)

Login-AzureRmAccount

Write-Output 'Deploying Azure Functions...'

$ScriptDirectory = [System.IO.Path]::GetDirectoryName($PSScriptRoot)
$ProjectPath = (get-item $ScriptDirectory).Parent.Parent.Parent.Parent.FullName
$RelativeFunctionPath = 'Thesis.MDM.AzureFunctions\bin\Debug\net461'
$FunctionPath = "$ProjectPath\$RelativeFunctionPath"
Write-Output $FunctionPath


$FileShareDestinationDirectory = 'site/wwwroot'
$StorageContex = New-AzureStorageContext $StorageAccountName $StorageAccountKey
$s = Get-AzureStorageShare $FileShareName -Context $StorageContex

$ArtifactFilePaths = Get-ChildItem $FunctionPath -Recurse -File | ForEach-Object -Process {$_.FullName}
$ArtifactDirectories = Get-ChildItem $FunctionPath -Recurse -Directory | ForEach-Object -Process {$_.FullName}

foreach ($Path in $ArtifactDirectories) {
		$temp = $Path.Substring($FunctionPath.length + 1)
		New-AzureStorageDirectory -Share $s -Path "$FileShareDestinationDirectory/$temp" -ErrorAction SilentlyContinue -ErrorVariable ErrorMessages
	}

if ($ErrorMessages) {
    Write-Output '', 'Template deployment returned the following errors:', @(@($ErrorMessages) | ForEach-Object { $_.Exception.Message.TrimEnd("`r`n") })
}	

foreach ($SourcePath in $ArtifactFilePaths) {
		$temp = $SourcePath.Substring($FunctionPath.length + 1)
		Set-AzureStorageFileContent -Share $s -Source $SourcePath -Path "$FileShareDestinationDirectory/$temp" -Force -ErrorVariable ErrorMessages		
		Write-Output $temp
	}

if ($ErrorMessages) {
    Write-Output '', 'Template deployment returned the following errors:', @(@($ErrorMessages) | ForEach-Object { $_.Exception.Message.TrimEnd("`r`n") })
}	

Write-Output 'Finished'
Read-Host -Prompt "Press Enter to exit"