$msb = "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
$Raiz = "C:\RM\Legado\12.1.2602\DataPrev"

$slns = Get-ChildItem $Raiz -Recurse -Filter *.sln -File |
  Where-Object { $_.Name -ne "BuildAll.sln" -and $_.FullName -notmatch 'node_modules' }

$geral = [Diagnostics.Stopwatch]::StartNew()
$res = @()
foreach ($s in $slns) {
  $sw = [Diagnostics.Stopwatch]::StartNew()
  $out = & $msb $s.FullName /t:restore /p:RestorePackagesConfig=true /nologo /v:quiet 2>&1
  $ok = ($LASTEXITCODE -eq 0)
  $res += [pscustomobject]@{
    Sln = $s.Name; OK = $ok; Seg = [math]::Round($sw.Elapsed.TotalSeconds,1)
  }
  $cor = if ($ok) { "Green" } else { "Red" }
  Write-Host ("{0,-45} {1,6}s  {2}" -f $s.Name, [math]::Round($sw.Elapsed.TotalSeconds,1), $(if($ok){"OK"}else{"FALHOU"})) -ForegroundColor $cor
  if (-not $ok) { $out | Select-Object -Last 8 | ForEach-Object { "      $_" } }
}
""
"TOTAL RESTORE: $([math]::Round($geral.Elapsed.TotalSeconds,1))s   ok=$(($res|Where-Object OK).Count)/$($res.Count)"
