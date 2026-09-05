<#
.SYNOPSIS
Installs the pinned, locally required UMA content into a VertexForm3D Unity checkout.

.DESCRIPTION
Copies UMA from a clean sibling UMA Git repository at the expected release commit. Disposable
sample and archival documentation content that is not required by the avatar integration is
omitted. Selected UMA authoring tools and their dependencies are retained even when they live
beneath a pruned sample tree. Assets/UMA and Assets/UMA.meta must be ignored by the destination
repository.

UMA_INSTALLED is intentionally not added while AnkleBreaker's optional UMA integration targets
the incompatible UMA 2 API. The retained define helper and call site should be revisited once
AnkleBreaker supports UMA 3.

If Assets/UMA already exists, pass -ReplaceExisting. The previous local UMA tree is moved to an
ignored Temp/UMA-Backup-* directory and retained for recovery.

.EXAMPLE
.\Tools\Sync-Uma.ps1 -WhatIf

Shows the planned installation when Assets/UMA does not already exist.

.EXAMPLE
.\Tools\Sync-Uma.ps1 -ReplaceExisting -WhatIf

Shows the planned replacement of an existing local UMA installation.

.EXAMPLE
.\Tools\Sync-Uma.ps1 -ReplaceExisting

Installs the pinned UMA content and keeps the previous installation under Temp.

.EXAMPLE
.\Tools\Sync-Uma.ps1 -UmaRepositoryPath E:\src\Unity\6000.3\UMA -ReplaceExisting

Uses an explicitly located UMA source checkout.
#>

[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter()]
    [string]$UmaRepositoryPath,

    [Parameter()]
    [string]$VertexProjectPath,

    [Parameter()]
    [string]$ExpectedCommit = '722b308aebfe5dee7d0048e24c1c464c00b5fc06',

    [Parameter()]
    [switch]$ReplaceExisting
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-RequiredDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        throw "$Description does not exist: $Path"
    }

    return (Resolve-Path -LiteralPath $Path).Path
}

function Assert-PathUnderRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Root
    )

    $absolutePath = [IO.Path]::GetFullPath($Path)
    $absoluteRoot = [IO.Path]::GetFullPath($Root).TrimEnd('\') + '\'
    if (-not $absolutePath.StartsWith($absoluteRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to operate outside the expected root '$absoluteRoot': $absolutePath"
    }
}

function Add-UnityScriptingDefine {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectSettingsPath,

        [Parameter(Mandatory = $true)]
        [string]$Define
    )

    $settingsText = [IO.File]::ReadAllText($ProjectSettingsPath)
    $headerMatch = [Text.RegularExpressions.Regex]::Match(
        $settingsText,
        '(?m)^  scriptingDefineSymbols:\r?$'
    )
    if (-not $headerMatch.Success) {
        throw "Unable to find scriptingDefineSymbols in '$ProjectSettingsPath'."
    }

    $sectionStart = $headerMatch.Index + $headerMatch.Length
    $remainingText = $settingsText.Substring($sectionStart)
    $nextSectionMatch = [Text.RegularExpressions.Regex]::Match(
        $remainingText,
        '(?m)^  \S[^:\r\n]*:.*\r?$'
    )
    $sectionLength = if ($nextSectionMatch.Success) {
        $nextSectionMatch.Index
    }
    else {
        $remainingText.Length
    }

    $sectionText = $remainingText.Substring(0, $sectionLength)
    $defineStats = [PSCustomObject]@{
        EntryCount = 0
        ChangedEntryCount = 0
    }
    $entryPattern = '(?m)^(?<prefix>    [^:\r\n]+:\s*)(?<symbols>[^\r\n]*)(?<newline>\r?\n|$)'
    $updatedSection = [Text.RegularExpressions.Regex]::Replace(
        $sectionText,
        $entryPattern,
        {
            param($match)

            $defineStats.EntryCount++
            $symbols = @(
                $match.Groups['symbols'].Value.Split(
                    [char[]]@(';'),
                    [StringSplitOptions]::RemoveEmptyEntries
                ) | ForEach-Object { $_.Trim() }
            )

            if ($symbols -ccontains $Define) {
                return $match.Value
            }

            $defineStats.ChangedEntryCount++
            $updatedSymbols = if ($symbols.Count -eq 0) {
                $Define
            }
            else {
                ($symbols + $Define) -join ';'
            }

            return (
                $match.Groups['prefix'].Value +
                $updatedSymbols +
                $match.Groups['newline'].Value
            )
        }
    )

    if ($defineStats.EntryCount -eq 0) {
        throw "No scripting define entries were found in '$ProjectSettingsPath'."
    }

    if ($defineStats.ChangedEntryCount -gt 0) {
        $updatedText = (
            $settingsText.Substring(0, $sectionStart) +
            $updatedSection +
            $settingsText.Substring($sectionStart + $sectionLength)
        )
        [IO.File]::WriteAllText(
            $ProjectSettingsPath,
            $updatedText,
            [Text.UTF8Encoding]::new($false)
        )
    }

    return [PSCustomObject]@{
        EntryCount = $defineStats.EntryCount
        ChangedEntryCount = $defineStats.ChangedEntryCount
    }
}

$toolsDirectory = Split-Path -Parent $PSCommandPath
$defaultVertexProject = Split-Path -Parent $toolsDirectory
$workspaceDirectory = Split-Path -Parent $defaultVertexProject

if ([string]::IsNullOrWhiteSpace($VertexProjectPath)) {
    $VertexProjectPath = $defaultVertexProject
}

if ([string]::IsNullOrWhiteSpace($UmaRepositoryPath)) {
    $UmaRepositoryPath = Join-Path $workspaceDirectory 'UMA'
}

$resolvedVertexProject = Resolve-RequiredDirectory `
    -Path $VertexProjectPath `
    -Description 'VertexForm3D Unity project'
$resolvedUmaRepository = Resolve-RequiredDirectory `
    -Path $UmaRepositoryPath `
    -Description 'UMA Git repository'

$projectVersionFile = Join-Path $resolvedVertexProject 'ProjectSettings\ProjectVersion.txt'
if (-not (Test-Path -LiteralPath $projectVersionFile -PathType Leaf)) {
    throw "The destination is not a Unity project: $resolvedVertexProject"
}
$projectSettingsFile = Join-Path $resolvedVertexProject 'ProjectSettings\ProjectSettings.asset'
if (-not (Test-Path -LiteralPath $projectSettingsFile -PathType Leaf)) {
    throw "Unity project settings are missing: $projectSettingsFile"
}

$sourceUma = Resolve-RequiredDirectory `
    -Path (Join-Path $resolvedUmaRepository 'UMAProject\Assets\UMA') `
    -Description 'UMA source asset directory'
$sourceUmaMeta = Join-Path $resolvedUmaRepository 'UMAProject\Assets\UMA.meta'
if (-not (Test-Path -LiteralPath $sourceUmaMeta -PathType Leaf)) {
    throw "UMA source metadata is missing: $sourceUmaMeta"
}
$sourceShaders = Resolve-RequiredDirectory `
    -Path (Join-Path $resolvedUmaRepository 'UMAProject\Assets\SourceShaders') `
    -Description 'UMA source shader directory'
$sourceShadersMeta = Join-Path $resolvedUmaRepository 'UMAProject\Assets\SourceShaders.meta'
if (-not (Test-Path -LiteralPath $sourceShadersMeta -PathType Leaf)) {
    throw "UMA source shader metadata is missing: $sourceShadersMeta"
}

$destinationAssets = Resolve-RequiredDirectory `
    -Path (Join-Path $resolvedVertexProject 'Assets') `
    -Description 'VertexForm3D Assets directory'
$destinationUma = Join-Path $destinationAssets 'UMA'
$destinationUmaMeta = Join-Path $destinationAssets 'UMA.meta'
$destinationSourceShaders = Join-Path $destinationAssets 'SourceShaders'
$destinationSourceShadersMeta = Join-Path $destinationAssets 'SourceShaders.meta'

Assert-PathUnderRoot -Path $destinationUma -Root $resolvedVertexProject
Assert-PathUnderRoot -Path $destinationUmaMeta -Root $resolvedVertexProject
Assert-PathUnderRoot -Path $destinationSourceShaders -Root $resolvedVertexProject
Assert-PathUnderRoot -Path $destinationSourceShadersMeta -Root $resolvedVertexProject

$gitCommand = Get-Command git -ErrorAction Stop
$robocopyCommand = Get-Command robocopy.exe -ErrorAction Stop

$actualCommit = (& $gitCommand.Source -C $resolvedUmaRepository rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to resolve the UMA source commit.'
}
if ($actualCommit -ne $ExpectedCommit) {
    throw "UMA source is at $actualCommit; expected $ExpectedCommit."
}

$sourceChanges = @(& $gitCommand.Source -C $resolvedUmaRepository status --porcelain)
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to inspect the UMA source working tree.'
}
if ($sourceChanges.Count -ne 0) {
    throw 'The UMA source working tree is not clean. Refusing to copy an unreproducible source tree.'
}

foreach ($ignoredPath in @(
    'Assets/UMA',
    'Assets/UMA.meta',
    'Assets/SourceShaders',
    'Assets/SourceShaders.meta'
)) {
    & $gitCommand.Source -C $resolvedVertexProject check-ignore --quiet --no-index -- $ignoredPath
    if ($LASTEXITCODE -ne 0) {
        throw "'$ignoredPath' is not ignored by the destination repository."
    }
}

if (
    (
        (Test-Path -LiteralPath $destinationUma) -or
        (Test-Path -LiteralPath $destinationSourceShaders)
    ) -and
    -not $ReplaceExisting
) {
    throw (
        "UMA or its SourceShaders already exist in '$destinationAssets'. " +
        'Re-run with -ReplaceExisting to replace them safely.'
    )
}

$excludedDirectories = @(
    (Join-Path $sourceUma 'Examples'),
    (Join-Path $sourceUma 'UMA3\Documentation'),
    (Join-Path $sourceUma 'UMA3\OLD_Getting Started'),
    (Join-Path $sourceUma 'UMA3\RandomCharacters'),
    (Join-Path $sourceUma 'UMA3\Scenes')
)

$removedOrphanMetadata = @(
    'Examples.meta',
    'UMAContentCreation.docx.meta',
    'UMA3/Documentation.meta',
    'UMA3/OLD_Getting Started.meta',
    'UMA3/RandomCharacters.meta'
)

# UMA keeps some production authoring tools beneath its sample-scene tree. The broad scene
# exclusion above is restored selectively from this pinned allowlist so those tools remain usable
# without importing the complete sample-scene payload.
$retainedAuthoringPaths = @(
    'UMA3/Scenes.meta',
    'UMA3/Scenes/Prefabs.meta',
    'UMA3/Scenes/Prefabs/Textures.meta',
    'UMA3/Scenes/Prefabs/Textures/BodyCamRenderTexture.renderTexture',
    'UMA3/Scenes/Prefabs/Textures/BodyCamRenderTexture.renderTexture.meta',
    'UMA3/Scenes/Prefabs/Textures/ChestCamRenderTexture.renderTexture',
    'UMA3/Scenes/Prefabs/Textures/ChestCamRenderTexture.renderTexture.meta',
    'UMA3/Scenes/Prefabs/Textures/FaceCamRenderTexture.renderTexture',
    'UMA3/Scenes/Prefabs/Textures/FaceCamRenderTexture.renderTexture.meta',
    'UMA3/Scenes/Prefabs/Textures/FeetCamRenderTexture.renderTexture',
    'UMA3/Scenes/Prefabs/Textures/FeetCamRenderTexture.renderTexture.meta',
    'UMA3/Scenes/Prefabs/Textures/HandsCamRenderTexture.renderTexture',
    'UMA3/Scenes/Prefabs/Textures/HandsCamRenderTexture.renderTexture.meta',
    'UMA3/Scenes/Prefabs/Textures/HeadCamRenderTexture.renderTexture',
    'UMA3/Scenes/Prefabs/Textures/HeadCamRenderTexture.renderTexture.meta',
    'UMA3/Scenes/Prefabs/Textures/LegsCamRenderTexture.renderTexture',
    'UMA3/Scenes/Prefabs/Textures/LegsCamRenderTexture.renderTexture.meta',
    'UMA3/Scenes/U3-Tools-Photobooth.meta',
    'UMA3/Scenes/U3-Tools-Photobooth.unity',
    'UMA3/Scenes/U3-Tools-Photobooth.unity.meta',
    'UMA3/Scenes/U3-Tools-Photobooth'
)

$operation = "Install UMA release $ExpectedCommit into '$destinationUma' and '$destinationSourceShaders'"
if (-not $PSCmdlet.ShouldProcess($resolvedVertexProject, $operation)) {
    return
}

$syncId = [Guid]::NewGuid().ToString('N')
$stagingRoot = Join-Path $resolvedVertexProject ("Temp\UMA-Sync-$syncId")
$stagingUma = Join-Path $stagingRoot 'UMA'
$stagingSourceShaders = Join-Path $stagingRoot 'SourceShaders'
$projectSettingsBackup = Join-Path $stagingRoot 'ProjectSettings.asset.before-uma-sync'
$backupRoot = $null
$backupUma = $null
$backupUmaMeta = $null
$backupSourceShaders = $null
$backupSourceShadersMeta = $null
$destinationWasMoved = $false
$metadataWasMoved = $false
$sourceShadersDestinationWasMoved = $false
$sourceShadersMetadataWasMoved = $false
$newUmaWasInstalled = $false
$newSourceShadersWereInstalled = $false
$projectSettingsBackupCreated = $false
$installCompleted = $false

Assert-PathUnderRoot -Path $stagingRoot -Root $resolvedVertexProject
[void](New-Item -ItemType Directory -Path $stagingUma -Force)

try {
    Copy-Item -LiteralPath $projectSettingsFile -Destination $projectSettingsBackup
    $projectSettingsBackupCreated = $true

    $robocopyArguments = @(
        $sourceUma,
        $stagingUma,
        '/E',
        '/COPY:DAT',
        '/DCOPY:DAT',
        '/R:2',
        '/W:1',
        '/NFL',
        '/NDL',
        '/NJH',
        '/NJS',
        '/NP',
        '/XD'
    ) + $excludedDirectories

    & $robocopyCommand.Source @robocopyArguments | Out-Null
    $robocopyExitCode = $LASTEXITCODE
    if ($robocopyExitCode -ge 8) {
        throw "Robocopy failed with exit code $robocopyExitCode."
    }

    Copy-Item -LiteralPath $sourceShaders -Destination $stagingSourceShaders -Recurse

    foreach ($relativePath in $retainedAuthoringPaths) {
        $sourcePath = Join-Path $sourceUma $relativePath.Replace('/', '\')
        if (-not (Test-Path -LiteralPath $sourcePath)) {
            throw "Required UMA authoring content is missing from the pinned source: $sourcePath"
        }

        $stagedPath = Join-Path $stagingUma $relativePath.Replace('/', '\')
        Assert-PathUnderRoot -Path $stagedPath -Root $stagingRoot
        $stagedParent = Split-Path -Parent $stagedPath
        [void](New-Item -ItemType Directory -Path $stagedParent -Force)
        Copy-Item -LiteralPath $sourcePath -Destination $stagedPath -Recurse
    }

    foreach ($relativePath in $removedOrphanMetadata) {
        $stagedPath = Join-Path $stagingUma $relativePath.Replace('/', '\')
        Assert-PathUnderRoot -Path $stagedPath -Root $stagingRoot
        if (Test-Path -LiteralPath $stagedPath) {
            Remove-Item -LiteralPath $stagedPath -Force
        }
    }

    foreach ($excludedDirectory in @(
        'Examples',
        'UMA3/Documentation',
        'UMA3/OLD_Getting Started',
        'UMA3/RandomCharacters'
    )) {
        $unexpectedPath = Join-Path $stagingUma $excludedDirectory.Replace('/', '\')
        if (Test-Path -LiteralPath $unexpectedPath) {
            throw "Excluded UMA directory was copied unexpectedly: $unexpectedPath"
        }
    }

    foreach ($requiredAuthoringPath in $retainedAuthoringPaths) {
        $stagedAuthoringPath = Join-Path `
            $stagingUma `
            $requiredAuthoringPath.Replace('/', '\')
        if (-not (Test-Path -LiteralPath $stagedAuthoringPath)) {
            throw "Required UMA authoring content was not copied: $stagedAuthoringPath"
        }
    }

    foreach ($requiredDocumentationPath in @(
        'Docs/!README.md',
        'Docs/UMAMaterial.md'
    )) {
        $stagedDocumentationPath = Join-Path $stagingUma $requiredDocumentationPath.Replace('/', '\')
        if (-not (Test-Path -LiteralPath $stagedDocumentationPath -PathType Leaf)) {
            throw "Required UMA documentation was not copied: $stagedDocumentationPath"
        }
    }

    foreach ($requiredSourceShaderPath in @(
        'SRPShaders/Alpha/UMA_DiffuseAlpha_Hair.surfshader',
        'SRPShaders/Opaque/UMA_Diffuse.surfshader'
    )) {
        $stagedSourceShaderPath = Join-Path $stagingSourceShaders $requiredSourceShaderPath.Replace('/', '\')
        if (-not (Test-Path -LiteralPath $stagedSourceShaderPath -PathType Leaf)) {
            throw "Required UMA source shader was not copied: $stagedSourceShaderPath"
        }
    }

    $stagedFiles = @(Get-ChildItem -LiteralPath $stagingUma -File -Recurse)
    if ($stagedFiles.Count -lt 4000) {
        throw "Staged UMA tree contains only $($stagedFiles.Count) files; refusing an incomplete install."
    }

    if (
        (Test-Path -LiteralPath $destinationUma) -or
        (Test-Path -LiteralPath $destinationSourceShaders)
    ) {
        $backupStamp = Get-Date -Format 'yyyyMMdd-HHmmss'
        $backupRoot = Join-Path $resolvedVertexProject "Temp\UMA-Backup-$backupStamp"
        $backupUma = Join-Path $backupRoot 'UMA'
        $backupUmaMeta = Join-Path $backupRoot 'UMA.meta'
        $backupSourceShaders = Join-Path $backupRoot 'SourceShaders'
        $backupSourceShadersMeta = Join-Path $backupRoot 'SourceShaders.meta'
        Assert-PathUnderRoot -Path $backupRoot -Root $resolvedVertexProject
        [void](New-Item -ItemType Directory -Path $backupRoot)

        if (Test-Path -LiteralPath $destinationUma) {
            Move-Item -LiteralPath $destinationUma -Destination $backupUma
            $destinationWasMoved = $true
        }

        if (Test-Path -LiteralPath $destinationUmaMeta) {
            Move-Item -LiteralPath $destinationUmaMeta -Destination $backupUmaMeta
            $metadataWasMoved = $true
        }

        if (Test-Path -LiteralPath $destinationSourceShaders) {
            Move-Item -LiteralPath $destinationSourceShaders -Destination $backupSourceShaders
            $sourceShadersDestinationWasMoved = $true
        }

        if (Test-Path -LiteralPath $destinationSourceShadersMeta) {
            Move-Item -LiteralPath $destinationSourceShadersMeta -Destination $backupSourceShadersMeta
            $sourceShadersMetadataWasMoved = $true
        }
    }

    Move-Item -LiteralPath $stagingUma -Destination $destinationUma
    $newUmaWasInstalled = $true
    Copy-Item -LiteralPath $sourceUmaMeta -Destination $destinationUmaMeta
    Move-Item -LiteralPath $stagingSourceShaders -Destination $destinationSourceShaders
    $newSourceShadersWereInstalled = $true
    Copy-Item -LiteralPath $sourceShadersMeta -Destination $destinationSourceShadersMeta

    # Temporarily disabled: AnkleBreaker's optional UMA commands target the UMA 2 API and do not
    # compile with UMA 3. Revisit this retained call once UMA 3 support is added to AnkleBreaker.
    # $defineResult = Add-UnityScriptingDefine `
    #     -ProjectSettingsPath $projectSettingsFile `
    #     -Define 'UMA_INSTALLED'
    $installCompleted = $true

    $installedFiles = @(Get-ChildItem -LiteralPath $destinationUma -File -Recurse)
    $installedBytes = ($installedFiles | Measure-Object -Property Length -Sum).Sum
    $installedSourceShaderFiles = @(
        Get-ChildItem -LiteralPath $destinationSourceShaders -File -Recurse
    )
    $installedSourceShaderBytes = (
        $installedSourceShaderFiles | Measure-Object -Property Length -Sum
    ).Sum

    Write-Output "Installed UMA base commit: $actualCommit"
    Write-Output "Installed files: $($installedFiles.Count)"
    Write-Output "Installed bytes: $installedBytes"
    Write-Output "Installed source shader files: $($installedSourceShaderFiles.Count)"
    Write-Output "Installed source shader bytes: $installedSourceShaderBytes"
    Write-Output 'Retained UMA authoring tool: UMA3/Scenes/U3-Tools-Photobooth.unity'
    Write-Output (
        'UMA_INSTALLED remains disabled pending UMA 3 support in AnkleBreaker; ' +
        'UMA itself does not consume this presence define.'
    )
    if ($null -ne $backupRoot) {
        Write-Output "Previous UMA backup retained at: $backupRoot"
    }
    Write-Output 'Next step: open the Unity project and rebuild the UMA Global Library.'
}
catch {
    if ($projectSettingsBackupCreated -and -not $installCompleted) {
        Copy-Item -LiteralPath $projectSettingsBackup -Destination $projectSettingsFile -Force
    }

    if (-not $installCompleted) {
        if ($newUmaWasInstalled) {
            if (Test-Path -LiteralPath $destinationUma) {
                Assert-PathUnderRoot -Path $destinationUma -Root $resolvedVertexProject
                Remove-Item -LiteralPath $destinationUma -Recurse -Force
            }
            if (Test-Path -LiteralPath $destinationUmaMeta) {
                Assert-PathUnderRoot -Path $destinationUmaMeta -Root $resolvedVertexProject
                Remove-Item -LiteralPath $destinationUmaMeta -Force
            }
        }

        if ($newSourceShadersWereInstalled) {
            if (Test-Path -LiteralPath $destinationSourceShaders) {
                Assert-PathUnderRoot -Path $destinationSourceShaders -Root $resolvedVertexProject
                Remove-Item -LiteralPath $destinationSourceShaders -Recurse -Force
            }
            if (Test-Path -LiteralPath $destinationSourceShadersMeta) {
                Assert-PathUnderRoot -Path $destinationSourceShadersMeta -Root $resolvedVertexProject
                Remove-Item -LiteralPath $destinationSourceShadersMeta -Force
            }
        }

        if ($destinationWasMoved -and (Test-Path -LiteralPath $backupUma)) {
            Move-Item -LiteralPath $backupUma -Destination $destinationUma
        }

        if ($metadataWasMoved -and (Test-Path -LiteralPath $backupUmaMeta)) {
            Move-Item -LiteralPath $backupUmaMeta -Destination $destinationUmaMeta
        }

        if (
            $sourceShadersDestinationWasMoved -and
            (Test-Path -LiteralPath $backupSourceShaders)
        ) {
            Move-Item -LiteralPath $backupSourceShaders -Destination $destinationSourceShaders
        }

        if (
            $sourceShadersMetadataWasMoved -and
            (Test-Path -LiteralPath $backupSourceShadersMeta)
        ) {
            Move-Item -LiteralPath $backupSourceShadersMeta -Destination $destinationSourceShadersMeta
        }
    }

    throw
}
finally {
    if (Test-Path -LiteralPath $stagingRoot) {
        Assert-PathUnderRoot -Path $stagingRoot -Root $resolvedVertexProject
        Remove-Item -LiteralPath $stagingRoot -Recurse -Force
    }
}
