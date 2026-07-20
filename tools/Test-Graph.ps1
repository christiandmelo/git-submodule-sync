param(
  [string]$Raiz = "C:\RM\Legado\12.1.2602\DataPrev",
  [switch]$IgnorarTestes
)

$ErrorActionPreference = "Stop"
$ns = @{ m = "http://schemas.microsoft.com/developer/msbuild/2003" }

Write-Host "Raiz: $Raiz" -ForegroundColor Cyan

# 1. Descoberta dos csproj
$csprojs = Get-ChildItem -Path $Raiz -Recurse -Filter *.csproj -File |
  Where-Object { $_.FullName -notmatch '\\(bin|obj|packages|\.git)\\' }

Write-Host "csproj encontrados: $($csprojs.Count)" -ForegroundColor Cyan

$nos = @()
foreach ($f in $csprojs) {
  [xml]$xml = Get-Content -LiteralPath $f.FullName -Raw

  $asm = (Select-Xml -Xml $xml -XPath "//m:AssemblyName" -Namespace $ns | Select-Object -First 1).Node.InnerText
  if ([string]::IsNullOrWhiteSpace($asm)) { $asm = [IO.Path]::GetFileNameWithoutExtension($f.Name) }

  # OutputPath do Debug (o que alimenta Bin\Custom)
  $outDebug = ""
  foreach ($pg in (Select-Xml -Xml $xml -XPath "//m:PropertyGroup" -Namespace $ns)) {
    $cond = $pg.Node.GetAttribute("Condition")
    if ($cond -match "Debug") {
      $op = Select-Xml -Xml $pg.Node -XPath "m:OutputPath" -Namespace $ns
      if ($op) { $outDebug = $op.Node.InnerText }
    }
  }

  $dir = $f.DirectoryName
  $sub = ($f.FullName.Substring($Raiz.Length).TrimStart('\') -split '\\')[0]

  # ProjectReference -> caminho absoluto
  $projRefs = @()
  foreach ($pr in (Select-Xml -Xml $xml -XPath "//m:ProjectReference" -Namespace $ns)) {
    $inc = $pr.Node.GetAttribute("Include")
    if ($inc) {
      $abs = [IO.Path]::GetFullPath((Join-Path $dir $inc))
      $projRefs += $abs
    }
  }

  # HintPath -> classificar
  $refCustom = @()   # dependencia entre submodulos (Bin\Custom)
  $refBinRM  = @()   # produto RM instalado (Bin\ raiz)
  $refPkg    = @()   # nuget packages\
  foreach ($hp in (Select-Xml -Xml $xml -XPath "//m:Reference/m:HintPath" -Namespace $ns)) {
    $raw = $hp.Node.InnerText
    if ([string]::IsNullOrWhiteSpace($raw)) { continue }
    try { $abs = [IO.Path]::GetFullPath((Join-Path $dir $raw)) } catch { continue }
    $nome = [IO.Path]::GetFileNameWithoutExtension($abs)
    if ($abs -match '\\Bin\\Custom\\')      { $refCustom += [pscustomobject]@{ Nome = $nome; Abs = $abs } }
    elseif ($abs -match '\\packages\\')     { $refPkg    += [pscustomobject]@{ Nome = $nome; Abs = $abs } }
    elseif ($abs -match '\\Bin\\')          { $refBinRM  += [pscustomobject]@{ Nome = $nome; Abs = $abs } }
  }

  $ehTeste = $asm -match '\.(TesteUnitario|TestesUnitarios|TesteUnitarios)$'

  $nos += [pscustomobject]@{
    Caminho    = $f.FullName
    Submodulo  = $sub
    Assembly   = $asm
    OutDebug   = $outDebug
    ProjRefs   = $projRefs
    RefCustom  = $refCustom
    RefBinRM   = $refBinRM
    RefPkg     = $refPkg
    EhTeste    = $ehTeste
  }
}

if ($IgnorarTestes) {
  $antes = $nos.Count
  $nos = $nos | Where-Object { -not $_.EhTeste }
  Write-Host "Projetos de teste ignorados: $($antes - $nos.Count)" -ForegroundColor DarkGray
}

# indices
$porCaminho = @{}
$porAssembly = @{}
foreach ($n in $nos) {
  $porCaminho[$n.Caminho.ToLowerInvariant()] = $n
  if (-not $porAssembly.ContainsKey($n.Assembly)) { $porAssembly[$n.Assembly] = $n }
}

Write-Host ""
Write-Host "=== OUTPUTPATH (Debug) ===" -ForegroundColor Yellow
$nos | Group-Object OutDebug | Sort-Object Count -Descending | ForEach-Object {
  "{0,4}x  '{1}'" -f $_.Count, $_.Name
}

# 2. Arestas
$arestas = @{}   # assembly -> lista de assemblies dos quais depende
$externosFaltando = @()
$customNaoResolvido = @()

foreach ($n in $nos) {
  $deps = New-Object System.Collections.Generic.HashSet[string]

  foreach ($p in $n.ProjRefs) {
    $k = $p.ToLowerInvariant()
    if ($porCaminho.ContainsKey($k)) { [void]$deps.Add($porCaminho[$k].Assembly) }
  }

  foreach ($r in $n.RefCustom) {
    if ($porAssembly.ContainsKey($r.Nome)) {
      if ($porAssembly[$r.Nome].Assembly -ne $n.Assembly) { [void]$deps.Add($r.Nome) }
    } else {
      $customNaoResolvido += [pscustomobject]@{ De = $n.Assembly; Alvo = $r.Nome; Arquivo = $r.Abs }
    }
  }

  foreach ($r in ($n.RefBinRM + $n.RefPkg)) {
    if (-not (Test-Path -LiteralPath $r.Abs)) {
      $externosFaltando += [pscustomobject]@{ De = $n.Assembly; Arquivo = $r.Abs }
    }
  }

  $arestas[$n.Assembly] = @($deps)
}

# 3. Kahn em ondas
$restantes = @{}
foreach ($k in $arestas.Keys) { $restantes[$k] = @($arestas[$k]) }

$ondas = @()
$resolvidos = New-Object System.Collections.Generic.HashSet[string]

while ($restantes.Count -gt 0) {
  $onda = @()
  foreach ($k in @($restantes.Keys)) {
    $pendentes = @($restantes[$k] | Where-Object { $restantes.ContainsKey($_) })
    if ($pendentes.Count -eq 0) { $onda += $k }
  }
  if ($onda.Count -eq 0) { break }   # ciclo
  foreach ($k in $onda) { $restantes.Remove($k); [void]$resolvidos.Add($k) }
  $ondas += ,@($onda | Sort-Object)
}

Write-Host ""
Write-Host "=== ONDAS DE BUILD ===" -ForegroundColor Yellow
$i = 1
foreach ($o in $ondas) {
  Write-Host ("Onda {0,2}: {1,4} projeto(s)" -f $i, $o.Count) -ForegroundColor Green
  $i++
}
Write-Host ("Total em ondas: {0} de {1}" -f (($ondas | ForEach-Object { $_.Count }) | Measure-Object -Sum).Sum, $nos.Count)

if ($restantes.Count -gt 0) {
  Write-Host ""
  Write-Host "=== CICLO DETECTADO ($($restantes.Count) projetos nao ordenaveis) ===" -ForegroundColor Red
  foreach ($k in ($restantes.Keys | Sort-Object | Select-Object -First 25)) {
    $pend = @($restantes[$k] | Where-Object { $restantes.ContainsKey($_) })
    "  {0}  ->  {1}" -f $k, ($pend -join ', ')
  }
}

# 4. Diagnostico
Write-Host ""
Write-Host "=== ONDA FINAL (ultimos a compilar) ===" -ForegroundColor Yellow
if ($ondas.Count -gt 0) { $ondas[-1] | ForEach-Object { "  $_" } }

Write-Host ""
Write-Host "=== ONDE ESTA O PLUGIN ===" -ForegroundColor Yellow
$i = 1
foreach ($o in $ondas) {
  foreach ($p in $o) {
    if ($p -match 'RM\.Cst\.DataPrev\.(Plugin|Const|Form|IServer|Server|CustomScript)$') {
      "  Onda {0,2}: {1}" -f $i, $p
    }
  }
  $i++
}

$plugin = $nos | Where-Object { $_.Assembly -eq 'RM.Cst.DataPrev.Plugin' }
if ($plugin) {
  Write-Host ""
  Write-Host "Dependencias diretas de RM.Cst.DataPrev.Plugin: $($arestas['RM.Cst.DataPrev.Plugin'].Count)" -ForegroundColor Cyan
  $arestas['RM.Cst.DataPrev.Plugin'] | Sort-Object | ForEach-Object { "  $_" }
}

Write-Host ""
Write-Host "=== HINTPATH Bin\Custom SEM PROJETO CORRESPONDENTE ($($customNaoResolvido.Count)) ===" -ForegroundColor Yellow
$customNaoResolvido | Group-Object Alvo | Sort-Object Count -Descending | Select-Object -First 20 | ForEach-Object {
  "{0,3}x  {1}" -f $_.Count, $_.Name
}

Write-Host ""
Write-Host "=== REFERENCIAS EXTERNAS AUSENTES NO DISCO ($($externosFaltando.Count)) ===" -ForegroundColor Yellow
$externosFaltando | Group-Object { [IO.Path]::GetFileName($_.Arquivo) } | Sort-Object Count -Descending | Select-Object -First 20 | ForEach-Object {
  "{0,3}x  {1}" -f $_.Count, $_.Name
}

# 5. Metrica de paralelismo
Write-Host ""
Write-Host "=== POTENCIAL DE PARALELISMO ===" -ForegroundColor Yellow
$maior = ($ondas | ForEach-Object { $_.Count } | Measure-Object -Maximum).Maximum
"Ondas: $($ondas.Count)  |  maior onda: $maior  |  media: {0:N1}" -f ((($ondas | ForEach-Object { $_.Count }) | Measure-Object -Average).Average)
"Nucleos disponiveis: $env:NUMBER_OF_PROCESSORS"
