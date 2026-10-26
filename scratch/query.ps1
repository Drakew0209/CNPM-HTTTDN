
Add-Type -AssemblyName 'System.Data'
$conn = New-Object System.Data.SqlClient.SqlConnection('Server=(localdb)\MSSQLLocalDB;Database=InternetCafeDB;Integrated Security=True;')
$conn.Open()

Write-Output "=== ACTIVE SESSIONS ==="
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT Session_ID, Customer_ID, Computer_ID, Start_Time, Applied_Hourly_Rate, Status FROM dbo.Usage_Sessions WHERE Status = 'Active'"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Output ($reader[0].ToString() + ' | Cust: ' + $reader[1] + ' | Comp: ' + $reader[2] + ' | Start: ' + $reader[3] + ' | Rate: ' + $reader[4] + ' | ' + $reader[5])
}
$reader.Close()

$conn.Close()
