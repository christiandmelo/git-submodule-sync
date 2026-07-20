param(
  [string]$Raiz = "C:\RM\Legado\12.1.2602\DataPrev",
  [int]$Paralelismo = 0,
  [switch]$IncluirTestes,
  [string]$Target = "Build"
)
$ErrorActionPreference = "Stop"
if ($Paralelismo -le 0) { $Paralelismo = [int]$env:NUMBER_OF_PROCESSORS }
$msb = "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
$logDir = Join-Path $PSScriptRoot "buildlogs"
if (Test-Path $logDir) { Remove-Item $logDir -Recurse -Force }
New-Item -ItemType Directory -Path $logDir | Out-Null

$ns = @{ m = "http://schemas.microsoft.com/developer/msbuild/2003" }

# ---------- grafo ----------
$csprojs = Get-ChildItem -Path $Raiz -Recurse -Filter *.csproj -File |
  Where-Object { $_.FullName -notmatch '\\(bin|obj|packages|\.git|node_modules)\\' }

$nos = @()
foreach ($f in $csprojs) {
  [xml]$xml = Get-Content -LiteralPath $f.FullName -Raw
  $asm = (Select-Xml -Xml $xml -XPath "//m:AssemblyName" -Namespace $ns | Select-Object -First 1).Node.InnerText
  if ([string]::IsNullOrWhiteSpace($asm)) { $asm = [IO.Path]::GetFileNameWithoutExtension($f.Name) }
  $dir = $f.DirectoryName
  $projRefs = @()
  foreach ($pr in (Select-Xml -Xml $xml -XPath "//m:ProjectReference" -Namespace $ns)) {
    $inc = $pr.Node.GetAttribute("Include")
    if ($inc) { $projRefs += [IO.Path]::GetFullPath((Join-Path $dir $inc)) }
  }
  $refCustom = @()
  foreach ($hp in (Select-Xml -Xml $xml -XPath "//m:Reference/m:HintPath" -Namespace $ns)) {
    $raw = $hp.Node.InnerText
    if ([string]::IsNullOrWhiteSpace($raw)) { continue }
    try { $abs = [IO.Path]::GetFullPath((Join-Path $dir $raw)) } catch { continue }
    if ($abs -match '\\Bin\\Custom\\') { $refCustom += [IO.Path]::GetFileNameWithoutExtension($abs) }
  }
  $nos += [pscustomobject]@{
    Caminho = $f.FullName; Assembly = $asm; ProjRefs = $projRefs; RefCustom = $refCustom
    EhTeste = ($asm -match '\.(TesteUnitario|TestesUnitarios|TesteUnitarios)$')
  }
}
if (-not $IncluirTestes) { $nos = $nos | Where-Object { -not $_.EhTeste } }

$porCaminho = @{}; $porAssembly = @{}
foreach ($n in $nos) { $porCaminho[$n.Caminho.ToLowerInvariant()] = $n; if(-not $porAssembly.ContainsKey($n.Assembly)){$porAssembly[$n.Assembly]=$n} }

$arestas = @{}
foreach ($n in $nos) {
  $d = New-Object System.Collections.Generic.HashSet[string]
  foreach ($p in $n.ProjRefs) { $k=$p.ToLowerInvariant(); if ($porCaminho.ContainsKey($k)) { [void]$d.Add($porCaminho[$k].Assembly) } }
  foreach ($r in $n.RefCustom) { if ($porAssembly.ContainsKey($r) -and $r -ne $n.Assembly) { [void]$d.Add($r) } }
  $arestas[$n.Assembly] = @($d)
}

$restantes = @{}; foreach ($k in $arestas.Keys) { $restantes[$k] = @($arestas[$k]) }
$ondas = @()
while ($restantes.Count -gt 0) {
  $onda = @()
  foreach ($k in @($restantes.Keys)) {
    if (@($restantes[$k] | Where-Object { $restantes.ContainsKey($_) }).Count -eq 0) { $onda += $k }
  }
  if ($onda.Count -eq 0) { break }
  foreach ($k in $onda) { $restantes.Remove($k) }
  $ondas += ,@($onda | Sort-Object)
}

Write-Host "Projetos: $($nos.Count)   Ondas: $($ondas.Count)   Paralelismo: $Paralelismo" -ForegroundColor Cyan
Write-Host ""

# ---------- build ----------
$falhou = @{}      # assembly -> motivo
$bloqueado = @{}
$tempos = @()
$geral = [Diagnostics.Stopwatch]::StartNew()

for ($w = 0; $w -lt $ondas.Count; $w++) {
  $onda = $ondas[$w]
  $swOnda = [Diagnostics.Stopwatch]::StartNew()

  # bloqueia quem depende de algo que falhou
  $aCompilar = @()
  foreach ($a in $onda) {
    $ruins = @($arestas[$a] | Where-Object { $falhou.ContainsKey($_) -or $bloqueado.ContainsKey($_) })
    if ($ruins.Count -gt 0) { $bloqueado[$a] = ($ruins -join ', ') } else { $aCompilar += $a }
  }

  Write-Host ("--- Onda $($w+1)/$($ondas.Count): $($aCompilar.Count) a compilar, $($onda.Count - $aCompilar.Count) bloqueado(s)") -ForegroundColor Yellow

  $fila = New-Object System.Collections.Queue
  $aCompilar | ForEach-Object { $fila.Enqueue($_) }
  $ativos = @()

  while ($fila.Count -gt 0 -or $ativos.Count -gt 0) {
    while ($ativos.Count -lt $Paralelismo -and $fila.Count -gt 0) {
      $asm = $fila.Dequeue()
      $no = $porAssembly[$asm]
      $log = Join-Path $logDir ("$asm.log")
      $psi = New-Object System.Diagnostics.ProcessStartInfo
      $psi.FileName = $msb
      $psi.Arguments = "`"$($no.Caminho)`" /t:$Target /p:Configuration=Debug /p:Platform=AnyCPU /p:BuildProjectReferences=false /nologo /v:minimal /m:1 /clp:NoSummary"
      $psi.UseShellExecute = $false
      $psi.RedirectStandardOutput = $true
      $psi.RedirectStandardError = $true
      $p = [System.Diagnostics.Process]::Start($psi)
      $ativos += [pscustomobject]@{ Asm=$asm; Proc=$p; Log=$log; SW=[Diagnostics.Stopwatch]::StartNew()
                                    OutTask=$p.StandardOutput.ReadToEndAsync(); ErrTask=$p.StandardError.ReadToEndAsync() }
    }
    Start-Sleep -Milliseconds 120
    $aindaAtivos = @()
    foreach ($a in $ativos) {
      if ($a.Proc.HasExited) {
        $saida = $a.OutTask.Result + $a.ErrTask.Result
        Set-Content -LiteralPath $a.Log -Value $saida -Encoding UTF8
        $seg = [math]::Round($a.SW.Elapsed.TotalSeconds,1)
        $tempos += [pscustomobject]@{ Asm=$a.Asm; Onda=$w+1; Seg=$seg; Exit=$a.Proc.ExitCode }
        if ($a.Proc.ExitCode -ne 0) {
          $falhou[$a.Asm] = "exit $($a.Proc.ExitCode)"
          Write-Host ("   [FALHOU] {0}  ({1}s)" -f $a.Asm, $seg) -ForegroundColor Red
        }
      } else { $aindaAtivos += $a }
    }
    $ativos = $aindaAtivos
  }
  Write-Host ("    onda concluida em {0}s" -f [math]::Round($swOnda.Elapsed.TotalSeconds,1)) -ForegroundColor DarkGray
}

# ---------- resumo ----------
$total = [math]::Round($geral.Elapsed.TotalSeconds,1)
Write-Host ""
Write-Host "==================== RESUMO ====================" -ForegroundColor Cyan
"Projetos no grafo : $($nos.Count)"
"Compilados OK     : $((($tempos | Where-Object Exit -eq 0)).Count)"
"Falharam          : $($falhou.Count)"
"Bloqueados        : $($bloqueado.Count)"
"TEMPO TOTAL BUILD : ${total}s  ($([math]::Round($total/60,1)) min)"
Write-Host ""
if ($falhou.Count -gt 0) {
  Write-Host "--- FALHAS ---" -ForegroundColor Red
  foreach ($k in ($falhou.Keys | Sort-Object)) { "  $k" }
}
if ($bloqueado.Count -gt 0) {
  Write-Host "--- BLOQUEADOS POR DEPENDENCIA ---" -ForegroundColor Yellow
  foreach ($k in ($bloqueado.Keys | Sort-Object)) { "  {0}  <- {1}" -f $k, $bloqueado[$k] }
}
Write-Host ""
"--- 10 projetos mais lentos ---"
$tempos | Sort-Object Seg -Descending | Select-Object -First 10 | ForEach-Object { "  {0,6}s  onda {1}  {2}" -f $_.Seg, $_.Onda, $_.Asm }
$tempos | Export-Csv (Join-Path $logDir "tempos.csv") -NoTypeInformation -Encoding UTF8
"Logs em: $logDir"
