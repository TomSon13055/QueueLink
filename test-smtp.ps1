# Test SMTP directly with System.Net.Mail
$username = "th9312242@gmail.com"
$password = "jbahvsvcbghtzcvh"
$to = "th9312242@gmail.com"

try {
    $msg = New-Object System.Net.Mail.MailMessage
    $msg.From = $username
    $msg.To.Add($to)
    $msg.Subject = "QueueLink SMTP Test"
    $msg.Body = "Testing SMTP from PowerShell"

    $smtp = New-Object System.Net.Mail.SmtpClient
    $smtp.Host = "smtp.gmail.com"
    $smtp.Port = 587
    $smtp.EnableSsl = $true
    $smtp.Credentials = New-Object System.Net.NetworkCredential($username, $password)

    Write-Host "[TRY] Connecting to smtp.gmail.com:587..."
    $smtp.Send($msg)
    Write-Host "[OK] Email sent!"
} catch {
    Write-Host "[ERROR] $($_.Exception.Message)"
}
