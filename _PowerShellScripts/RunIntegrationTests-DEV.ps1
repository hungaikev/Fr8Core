$RootDir = Split-Path -parent $PSCommandPath
$HealthMonitorCmd = "$RootDir\..\Tests\HealthMonitor\bin\$(BuildConfiguration)\HealthMonitor.exe --email-report --app-name VSO-DEV --ensure-startup"
$SrcConfigFile = "$RootDir\DEV-HealthMonitor.exe.config"
$DstConfigFile = "$RootDir\..\Tests\HealthMonitor\bin\$(BuildConfiguration)\HealthMonitor.exe.config"

Write-Host "Copying HealthMonitor config file"
Copy-Item $SrcConfigFile -Destination $DstConfigFile -Force

Write-Host $HealthMonitorCmd

Start-Sleep -s 100
Invoke-Expression $HealthMonitorCmd
