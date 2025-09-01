# CraftableLuciferium Build Script
# This script compiles the RimWorld mod from the command line

param(
    [string]$Configuration = "Debug",
    [switch]$Clean,
    [switch]$Help
)

# Show help if requested
if ($Help) {
    Write-Host "CraftableLuciferium Build Script" -ForegroundColor Cyan
    Write-Host "Usage: .\build.ps1 [options]" -ForegroundColor White
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Yellow
    Write-Host "  -Configuration <config>  Build configuration (Debug/Release) [Default: Debug]"
    Write-Host "  -Clean                   Clean previous build outputs"
    Write-Host "  -Help                    Show this help message"
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Green
    Write-Host "  .\build.ps1                    # Build Debug configuration"
    Write-Host "  .\build.ps1 -Configuration Release  # Build Release configuration"
    Write-Host "  .\build.ps1 -Clean            # Clean and build"
    exit 0
}

# Configuration
$ProjectName = "CraftableLuciferium"
$SourceDir = "CraftableLuciferium"
$OutputDir = "..\..\Assemblies\"
$CompilerPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$RimWorldPath = "C:\Program Files (x86)\Steam\steamapps\common\RimWorld"
$AssemblyCSharpPath = "$RimWorldPath\RimWorldWin64_Data\Managed\Assembly-CSharp.dll"
$UnityEnginePath = "$RimWorldPath\RimWorldWin64_Data\Managed\UnityEngine.dll"

# Colors for output
$Colors = @{
    Info = "Cyan"
    Success = "Green"
    Warning = "Yellow"
    Error = "Red"
}

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Colors[$Color]
}

function Write-Info { param([string]$Message) Write-ColorOutput $Message "Info" }
function Write-Success { param([string]$Message) Write-ColorOutput $Message "Success" }
function Write-Warning { param([string]$Message) Write-ColorOutput $Message "Warning" }
function Write-Error { param([string]$Message) Write-ColorOutput $Message "Error" }

# Header
Write-Info "=========================================="
Write-Info "  CraftableLuciferium Build Script"
Write-Info "=========================================="
Write-Info "Configuration: $Configuration"
Write-Info "Source Directory: $SourceDir"
Write-Info "Output Directory: $OutputDir"
Write-Info ""

# Check prerequisites
Write-Info "Checking prerequisites..."

# Check if we're in the right directory
if (-not (Test-Path $SourceDir)) {
    Write-Error "Source directory '$SourceDir' not found. Please run this script from the project root."
    exit 1
}

# Check if compiler exists
if (-not (Test-Path $CompilerPath)) {
    Write-Error "C# compiler not found at: $CompilerPath"
    Write-Error "Please ensure .NET Framework 4.0+ is installed."
    exit 1
}

# Check if RimWorld assemblies exist
if (-not (Test-Path $AssemblyCSharpPath)) {
    Write-Error "RimWorld Assembly-CSharp.dll not found at: $AssemblyCSharpPath"
    Write-Error "Please ensure RimWorld is installed via Steam."
    exit 1
}

if (-not (Test-Path $UnityEnginePath)) {
    Write-Error "UnityEngine.dll not found at: $UnityEnginePath"
    Write-Error "Please ensure RimWorld is installed via Steam."
    exit 1
}

Write-Success "All prerequisites met!"

# Clean if requested
if ($Clean) {
    Write-Info "Cleaning previous build outputs..."
    $CleanPaths = @(
        "$SourceDir\*.dll",
        "$SourceDir\*.pdb",
        "$SourceDir\*.exe"
    )
    
    foreach ($Path in $CleanPaths) {
        if (Test-Path $Path) {
            Get-ChildItem $Path | Remove-Item -Force
            Write-Info "Cleaned: $Path"
        }
    }
    Write-Success "Clean completed!"
    Write-Info ""
}

# Create output directory if it doesn't exist
Write-Info "Current working directory: $(Get-Location)"
Write-Info "Output directory (relative): $OutputDir"
$AbsoluteOutputDir = (Resolve-Path $OutputDir -ErrorAction SilentlyContinue).Path
if (-not $AbsoluteOutputDir) {
    $AbsoluteOutputDir = Join-Path (Get-Location) $OutputDir
}
Write-Info "Output directory (absolute): $AbsoluteOutputDir"

if (-not (Test-Path $AbsoluteOutputDir)) {
    Write-Info "Creating output directory: $AbsoluteOutputDir"
    New-Item -ItemType Directory -Path $AbsoluteOutputDir -Force | Out-Null
}

# Build the project
Write-Info "Building $ProjectName..."
Write-Info ""

# Build command
$BuildArgs = @(
    "/target:library",
    "/out:$SourceDir\$ProjectName.dll",
    "/reference:`"$AssemblyCSharpPath`"",
    "/reference:`"$UnityEnginePath`"",
    "/reference:System.dll",
    "/reference:System.Core.dll",
    "/reference:System.Xml.dll",
    "/reference:System.Data.dll",
    "$SourceDir\*.cs",
    "$SourceDir\Properties\*.cs"
)

Write-Info "Running: $CompilerPath $($BuildArgs -join ' ')"
Write-Info ""

# Execute compilation
& $CompilerPath @BuildArgs

if ($LASTEXITCODE -eq 0) {
    Write-Info ""
    Write-Success "Build completed successfully!"
    Write-Info "Build exit code check passed, proceeding to copy..."
    
    # Show build results
    $DllPath = Join-Path (Get-Location) "$SourceDir\$ProjectName.dll"
    Write-Info "Looking for DLL at: $DllPath"
    Write-Info "DLL exists: $(Test-Path $DllPath)"
    
    if (Test-Path $DllPath) {
        $DllInfo = Get-Item $DllPath
        Write-Info "Output: $($DllInfo.FullName)"
        Write-Info "Size: $([math]::Round($DllInfo.Length / 1KB, 2)) KB"
        Write-Info "Modified: $($DllInfo.LastWriteTime)"
        
        # Copy to output directory
        $OutputPath = Join-Path $AbsoluteOutputDir "$ProjectName.dll"
        Write-Info "Copying from: $DllPath"
        Write-Info "Copying to: $OutputPath"
        Write-Info "Output directory exists: $(Test-Path $AbsoluteOutputDir)"
        Write-Info "Starting copy operation..."
        Copy-Item $DllPath $OutputPath -Force
        Write-Info "Copy operation completed"
        if (Test-Path $OutputPath) {
            Write-Success "Successfully copied to: $OutputPath"
        } else {
            Write-Error "Failed to copy to: $OutputPath"
        }
    } else {
        Write-Error "DLL not found at expected location: $DllPath"
    }
    
    Write-Info ""
    Write-Success "Your RimWorld mod is ready to use!"
    
} else {
    Write-Error "Build failed with exit code: $LASTEXITCODE"
    exit 1
}

Write-Info ""
Write-Info "Build script completed."
