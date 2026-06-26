@echo off
REM Windows 本地一键启动

set ROOT=%~dp0
cd /d %ROOT%

echo ==== 1. 启动 PostgreSQL (Docker) ====
docker compose up -d postgres redis
if errorlevel 1 goto :err

echo ==== 2. 等待数据库 ====
:wait_db
docker exec ai-cs-postgres pg_isready -U postgres >nul 2>&1
if errorlevel 1 (
  timeout /t 1 /nobreak >nul
  goto :wait_db
)

echo ==== 3. 启动 .NET API ====
start "API" cmd /k "cd backend\src\AiCustomerService.Api && set ASPNETCORE_ENVIRONMENT=Development && set ASPNETCORE_URLS=http://+:5000 && dotnet run"

echo ==== 4. 启动 Vue 后台 ====
start "Admin" cmd /k "cd frontend\admin && npm install && npm run dev"

echo ==== 5. 启动 uni-app ====
start "MiniApp" cmd /k "cd frontend\miniprogram && npm install && npm run dev:h5"

echo.
echo ==== 启动完成 ====
echo API:        http://localhost:5000
echo Admin:      http://localhost:5173
echo MiniApp:    http://localhost:5174
echo Hangfire:   http://localhost:5000/hangfire
echo OpenAPI:    http://localhost:5000/openapi/v1.json
pause
exit /b 0

:err
echo Docker 启动失败
exit /b 1