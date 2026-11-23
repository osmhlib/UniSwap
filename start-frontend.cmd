@echo off
setlocal
cd /d "%~dp0uniswap-frontend"

if not exist "node_modules\vite" (
    echo Frontend dependencies are missing. Run npm ci in uniswap-frontend first.
    pause
    exit /b 1
)

echo Starting UniSwap frontend at http://localhost:5173
echo Press Ctrl+C to stop.
call npm run dev -- --port 5173 --strictPort
if errorlevel 1 (
    echo Frontend stopped with an error. Check whether port 5173 is already in use.
    pause
    exit /b 1
)
