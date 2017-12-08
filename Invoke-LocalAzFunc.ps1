[CmdletBinding()]
Param(
    [int]$Port = 7071
)

DynamicParam {

    $sdkOutFile = Get-ChildItem functionsSdk.out -Recurse | Sort-Object -Property LastWriteTimeUtc -Descending | Select-Object -First 1
    if ($sdkOutFile -eq $null) {
        Write-Host "Couldn't find `"functionsSdk.out`" file." -ForegroundColor Yellow
        exit
    }
    $funcNames = Get-Content $sdkOutFile | ForEach-Object { $_ -replace "\\function.json", ""} | Sort-Object
    
    $attributes = New-Object System.Management.Automation.ParameterAttribute
    $attributesCollection = New-Object 'Collections.ObjectModel.Collection[System.Attribute]'
    $attributesCollection.Add($attributes)

    $validateSetAttributes = New-Object System.Management.Automation.ValidateSetAttribute ($funcNames)
    $attributesCollection.Add($validateSetAttributes)

    $runtimeDefinedParameter = New-Object -TypeName System.Management.Automation.RuntimeDefinedParameter @('FuncName', [System.String], $attributesCollection)
    $dictionary = New-Object System.Management.Automation.RuntimeDefinedParameterDictionary
    $dictionary.Add('FuncName', $runtimeDefinedParameter)

    return $dictionary
}

Begin {
    $FuncName = $PSBoundParameters.FuncName
    
    function Invoke-LocalAzFunc($funcName) {
        Write-Host "Run $funcName..." -ForegroundColor Yellow

        $url = "http://localhost:$Port/admin/functions/$funcName"
        Write-Host "HTTP POST $url" -ForegroundColor Gray
        Invoke-RestMethod -Method Post -Uri $url -Body "{}"

        Write-Host "done." -ForegroundColor Yellow
        Write-Host ""
    }
}

Process {
    if ($FuncName -eq $null) {
        for (; ; ) {

            for ($i = 0; $i -lt $funcNames.Length; $i++) {
                "{0}) {1}" -f ($i + 1), $funcNames[$i] | Write-Host -ForegroundColor Cyan
            }

            $input = Read-Host -Prompt "Chose number of functions"
            if (($input -match "^\d+$") -eq $false) { continue }
            $numOfFunc = [int]$input
            if (($numOfFunc -lt 1) -or ($funcNames.Length -lt $numOfFunc)) { continue }
    
            $funcName = $funcNames[$numOfFunc - 1]
    
            Invoke-LocalAzFunc $funcName    
        }
    }
    else {
        Invoke-LocalAzFunc $FuncName   
    }
}

