param([string]$targetPath)

if (-not $targetPath -or -not (Test-Path $targetPath)) {
    exit 0
}

try {
    $cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $cert) {
        $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=DevCert" -CertStoreLocation "Cert:\CurrentUser\My"
    }
    if ($cert) {
        Set-AuthenticodeSignature -FilePath $targetPath -Certificate $cert -ErrorAction SilentlyContinue | Out-Null
    }
    Unblock-File -Path $targetPath -ErrorAction SilentlyContinue
} catch {
    # Không làm gián đoạn quá trình build nếu có ngoại lệ
}
