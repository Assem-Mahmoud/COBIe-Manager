# Force reload of assembly
[System.Reflection.Assembly]::ReflectionOnlyLoadFrom('C:\Program Files\Autodesk\Revit 2025\RevitAPI.dll') | Out-Null

$dllPath = 'C:\Users\Assem\source\repos\RevitCadConverter\bin\Debug2025\RevitCadConverter.dll'
Write-Host "`nChecking: $dllPath" -ForegroundColor Cyan
Write-Host "File timestamp: $((Get-Item $dllPath).LastWriteTime)" -ForegroundColor Gray

$dll2025 = [System.Reflection.Assembly]::ReflectionOnlyLoadFrom($dllPath)
$revitApiRef2025 = $dll2025.GetReferencedAssemblies() | Where-Object { $_.Name -eq 'RevitAPI' }
Write-Host "`nRevitAPI Reference in Debug2025 build:" -ForegroundColor Cyan
Write-Host "  Name: $($revitApiRef2025.Name)" -ForegroundColor Green
Write-Host "  Version: $($revitApiRef2025.Version)" -ForegroundColor Green
Write-Host "  FullName: $($revitApiRef2025.FullName)" -ForegroundColor Yellow
