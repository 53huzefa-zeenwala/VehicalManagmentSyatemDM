#Requires -Version 7
<#
.SYNOPSIS
    Creates a small set of TEST master data through the API: vehicle categories and types, two lots,
    destinations, five vehicles and two staff drivers. Everything it creates is named "Test …" / "TEST-…".

.DESCRIPTION
    Signs in as an admin (this also checks that the admin's sign-in works), then creates each item only if
    an item with the same name, code or registration number doesn't exist yet, so it is safe to run again.
    It goes through the API, so every rule, ID and audit entry is the same as in the app.
    The admin password is asked for on screen and never stored. New test staff get one random password,
    printed once at the end.

.EXAMPLE
    pwsh -File tools/seed-test-data.ps1 -BaseUrl https://<server>:8445 -LoginId ADM2600001
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $BaseUrl,
    [Parameter(Mandatory)] [string] $LoginId,
    # Optional, for unattended runs; otherwise the password is asked for.
    [securestring] $Password
)

$ErrorActionPreference = 'Stop'
$api = $BaseUrl.TrimEnd('/') + '/api/v1'
$session = $null

function Invoke-Api {
    param([string] $Method, [string] $Path, $Body)
    $request = @{
        Method               = $Method
        Uri                  = "$api/$Path"
        WebSession           = $script:session
        SkipCertificateCheck = $true # the server uses a self-signed certificate (D-036)
        ContentType          = 'application/json'
    }
    if ($null -ne $Body) { $request.Body = $Body | ConvertTo-Json -Depth 6 }
    try {
        return Invoke-RestMethod @request
    }
    catch {
        $detail = $_.ErrorDetails.Message
        throw "$Method $Path failed: $(if ($detail) { $detail } else { $_.Exception.Message })"
    }
}

function Write-Step([string] $Text, [bool] $Created) {
    $mark = if ($Created) { '+ created ' } else { '= exists  ' }
    Write-Host "$mark $Text"
}

# --- Sign in -------------------------------------------------------------------------------------
if (-not $Password) { $Password = Read-Host "Password for $LoginId" -AsSecureString }
$plain = ConvertFrom-SecureString $Password -AsPlainText
try {
    # If IIS has just started the app, the API answers 503 DATABASE_NOT_READY for a few seconds: wait and retry.
    for ($attempt = 1; ; $attempt++) {
        try {
            $me = Invoke-RestMethod -Method Post -Uri "$api/auth/login" -SessionVariable session -SkipCertificateCheck `
                -ContentType 'application/json' -Body (@{ loginId = $LoginId; password = $plain } | ConvertTo-Json)
            break
        }
        catch {
            $starting = $_.Exception.Response.StatusCode.value__ -eq 503 -and $_.ErrorDetails.Message -match 'DATABASE_NOT_READY'
            if (-not $starting -or $attempt -ge 12) {
                throw "Sign-in failed: $(if ($_.ErrorDetails.Message) { $_.ErrorDetails.Message } else { $_.Exception.Message })"
            }
            Write-Host "API is starting up; retrying in 5 s ($attempt/12)..."
            Start-Sleep -Seconds 5
        }
    }
}
finally { $plain = $null }
$script:session = $session
if ($me.role -ne 'Admin') { throw "$LoginId is not an admin." }
Write-Host "Signed in as $($me.loginId) ($($me.fullName)). Sign-in works." -ForegroundColor Green

# --- Vehicle categories and types -------------------------------------------------------------------
$categorySpec = [ordered]@{
    'Test Car'  = @('Test Sedan', 'Test SUV')
    'Test Bike' = @('Test Scooter', 'Test Motorbike')
}
$typeIds = @{}
foreach ($categoryName in $categorySpec.Keys) {
    $category = (Invoke-Api GET 'vehicle-categories') | Where-Object name -eq $categoryName
    Write-Step "category $categoryName" (-not $category)
    if (-not $category) { $category = Invoke-Api POST 'vehicle-categories' @{ name = $categoryName } }
    foreach ($typeName in $categorySpec[$categoryName]) {
        $type = $category.types | Where-Object name -eq $typeName
        Write-Step "  type $typeName" (-not $type)
        if (-not $type) { $type = Invoke-Api POST "vehicle-categories/$($category.id)/types" @{ name = $typeName } }
        $typeIds[$typeName] = $type.id
    }
}

# --- Lots -----------------------------------------------------------------------------------------
$lotSpec = @(
    @{ code = 'TEST-A'; name = 'Test Lot A'; address = 'Test address A, Downtown Dubai'; latitude = 25.1972; longitude = 55.2744
       opensAt = '06:00'; closesAt = '23:00'; lotType = 'Open'; totalCapacity = 50
       caps = @{ 'Test Sedan' = 20; 'Test SUV' = 10; 'Test Scooter' = 10; 'Test Motorbike' = 10 } }
    @{ code = 'TEST-B'; name = 'Test Lot B'; address = 'Test address B, Dubai Marina'; latitude = 25.0805; longitude = 55.1403
       opensAt = $null; closesAt = $null; lotType = 'Underground'; totalCapacity = 30
       caps = @{ 'Test Sedan' = 15; 'Test SUV' = 5; 'Test Scooter' = 5; 'Test Motorbike' = 5 } }
)
$lotIds = @{}
$existingLots = Invoke-Api GET 'lots?includeInactive=true'
foreach ($spec in $lotSpec) {
    $lot = $existingLots | Where-Object code -eq $spec.code
    Write-Step "lot $($spec.code) $($spec.name)" (-not $lot)
    if (-not $lot) {
        $lot = Invoke-Api POST 'lots' @{
            code = $spec.code; name = $spec.name; address = $spec.address
            latitude = $spec.latitude; longitude = $spec.longitude
            contactName = 'Test Contact'; contactNumber = '+971500000001'
            opensAt = $spec.opensAt; closesAt = $spec.closesAt; lotType = $spec.lotType
            totalCapacity = $spec.totalCapacity; baselineOccupied = 0
            typeCapacities = @($spec.caps.GetEnumerator() | ForEach-Object {
                @{ vehicleTypeId = $typeIds[$_.Key]; capacity = $_.Value; baselineOccupied = 0 } })
        }
    }
    $lotIds[$spec.code] = $lot.id
}

# --- Destinations -----------------------------------------------------------------------------------
$areaSpec = [ordered]@{
    'Test Downtown' = @('Test Mall Entrance', 'Test Office Tower')
    'Test Marina'   = @('Test Hotel Lobby', 'Test Beach Gate')
}
foreach ($areaName in $areaSpec.Keys) {
    $area = (Invoke-Api GET 'areas') | Where-Object name -eq $areaName
    Write-Step "area $areaName" (-not $area)
    if (-not $area) { $area = Invoke-Api POST 'areas' @{ name = $areaName } }
    foreach ($locationName in $areaSpec[$areaName]) {
        $exists = $area.locations | Where-Object name -eq $locationName
        Write-Step "  location $locationName" (-not $exists)
        if (-not $exists) { Invoke-Api POST "areas/$($area.id)/locations" @{ name = $locationName } | Out-Null }
    }
}

# --- Vehicles ---------------------------------------------------------------------------------------
$vehicleSpec = @(
    @{ reg = 'TEST-1001'; type = 'Test Sedan';   brand = 'Toyota'; model = 'Camry';    colour = 'White';  lot = 'TEST-A'; status = 'Available' }
    @{ reg = 'TEST-1002'; type = 'Test SUV';     brand = 'Nissan'; model = 'Patrol';   colour = 'Black';  lot = 'TEST-A'; status = 'Available' }
    @{ reg = 'TEST-1003'; type = 'Test Sedan';   brand = 'Honda';  model = 'Accord';   colour = 'Silver'; lot = 'TEST-B'; status = 'Available' }
    @{ reg = 'TEST-1004'; type = 'Test Scooter'; brand = 'Yamaha'; model = 'NMAX';     colour = 'Blue';   lot = 'TEST-B'; status = 'Available' }
    @{ reg = 'TEST-1005'; type = 'Test SUV';     brand = 'Kia';    model = 'Sportage'; colour = 'Grey';   lot = 'TEST-B'; status = 'UnderMaintenance' }
)
foreach ($spec in $vehicleSpec) {
    $vehicle = (Invoke-Api GET "vehicles?search=$($spec.reg)&pageSize=50").items | Where-Object registrationNumber -eq $spec.reg
    Write-Step "vehicle $($spec.reg) $($spec.brand) $($spec.model) at $($spec.lot) ($($spec.status))" (-not $vehicle)
    if (-not $vehicle) {
        Invoke-Api POST 'vehicles' @{
            registrationNumber = $spec.reg; vehicleTypeId = $typeIds[$spec.type]
            brand = $spec.brand; model = $spec.model; colour = $spec.colour
            lotId = $lotIds[$spec.lot]; status = $spec.status
        } | Out-Null
    }
}

# --- Staff (drivers) ---------------------------------------------------------------------------------
$staffSpec = @(
    @{ name = 'Test Driver One'; mobile = '+971500000011'; licence = 'TEST-LIC-001' }
    @{ name = 'Test Driver Two'; mobile = '+971500000012'; licence = 'TEST-LIC-002' }
)
$staffPassword = $null
$newStaff = @()
foreach ($spec in $staffSpec) {
    $staff = (Invoke-Api GET "staff?search=$([uri]::EscapeDataString($spec.name))&pageSize=50").items | Where-Object fullName -eq $spec.name
    Write-Step "staff $($spec.name)$(if ($staff) { " ($($staff.loginId))" })" (-not $staff)
    if (-not $staff) {
        if (-not $staffPassword) {
            $staffPassword = 'Test-' + (-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 10 | ForEach-Object { [char]$_ }))
        }
        $staff = Invoke-Api POST 'staff' @{
            fullName = $spec.name; mobile = $spec.mobile; licenceNumber = $spec.licence; password = $staffPassword
        }
        $newStaff += $staff.loginId
    }
}

Invoke-Api POST 'auth/logout' | Out-Null
Write-Host ''
Write-Host 'Test data ready.' -ForegroundColor Green
if ($newStaff.Count -gt 0) {
    Write-Host "New test staff: $($newStaff -join ', ')  password: $staffPassword" -ForegroundColor Yellow
    Write-Host 'Note this password now; it is not shown again.' -ForegroundColor Yellow
}
