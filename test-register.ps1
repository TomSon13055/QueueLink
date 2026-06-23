# Test script: đăng ký + gửi OTP thật qua Gmail
$base = "http://localhost:5180"

# 1. Lấy trang Register để extract anti-CSRF token
$r = Invoke-WebRequest -Uri "$base/Account/Register" -SessionVariable wb -UseBasicParsing
$token = $r.InputFields | Where-Object { $_.name -eq "__RequestVerificationToken" } | Select-Object -ExpandProperty value
if (-not $token) { Write-Host "[ERROR] No anti-CSRF token found"; exit 1 }

Write-Host "[OK] Got anti-CSRF token: $($token.Substring(0,20))..."

# 2. POST Register
$body = @{
    __RequestVerificationToken = $token
    FullName                  = "Test User QueueLink"
    Email                     = "th9312242+$(Get-Date -Format 'HHmmss')@gmail.com"
    Phone                     = "0901234567"
    Password                  = "Test@123"
    ConfirmPassword           = "Test@123"
}

$r2 = Invoke-WebRequest -Uri "$base/Account/Register" -WebSession $wb -Method POST -Body $body -UseBasicParsing -MaximumRedirection 0 -ErrorAction SilentlyContinue
Write-Host "Register POST status: $($r2.StatusCode)"
Write-Host "Redirect location: $($r2.Headers['Location'])"
