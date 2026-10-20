<#
.SYNOPSIS
    Creates Vardiologio.App\admin.json: salt and hash of the administrator's password.

.DESCRIPTION
    The password is asked for on screen (hidden) and is never written anywhere; only its
    PBKDF2 salt and hash go into the file. The parameters match
    Vardiologio.Infrastructure/Auth/PasswordHasher.cs: SHA-256, 100 000 iterations,
    16-byte salt, 32-byte hash. admin.json is git-ignored: keep it out of the repository.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\New-AdminFile.ps1
#>

$ErrorActionPreference = 'Stop'

# Ask twice, hidden, and compare.
$first  = Read-Host 'Administrator password' -AsSecureString
$second = Read-Host 'Repeat the password'    -AsSecureString

function ConvertTo-Plain([Security.SecureString] $secure) {
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
}

$password = ConvertTo-Plain $first
if ($password -cne (ConvertTo-Plain $second)) { throw 'The two passwords are not the same.' }
if ($password.Length -lt 12) { throw 'Use at least 12 characters: the hash ships inside every installer.' }

# Random salt, then PBKDF2 with the same parameters as PasswordHasher.cs.
$salt = New-Object byte[] 16
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($salt)
$kdf = New-Object Security.Cryptography.Rfc2898DeriveBytes(
    [Text.Encoding]::UTF8.GetBytes($password), $salt, 100000,
    [Security.Cryptography.HashAlgorithmName]::SHA256)
$hash = $kdf.GetBytes(32)

# Written next to Vardiologio.App.csproj, as UTF-8 without BOM.
$target = Join-Path $PSScriptRoot '..\Vardiologio.App\admin.json'
$json = '{ "salt": "' + [Convert]::ToBase64String($salt) + '", "hash": "' + [Convert]::ToBase64String($hash) + '" }'
[IO.File]::WriteAllText($target, $json, (New-Object Text.UTF8Encoding($false)))

Write-Host "Written: $([IO.Path]::GetFullPath($target))"
