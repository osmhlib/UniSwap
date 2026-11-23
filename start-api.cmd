@echo off
setlocal
cd /d "%~dp0"

if not defined ConnectionStrings__UniSwap set "ConnectionStrings__UniSwap=Host=127.0.0.1;Port=5432;Database=uniswap_test;Username=postgres"
if not defined Cors__AllowedOrigins__0 set "Cors__AllowedOrigins__0=http://localhost:5173"

echo Starting UniSwap API at https://localhost:7258
echo Press Ctrl+C to stop.
dotnet run --project UniSwap.WebApi --launch-profile https
if errorlevel 1 (
    echo API stopped with an error. Check PostgreSQL and the messages above.
    pause
    exit /b 1
)
