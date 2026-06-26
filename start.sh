#!/usr/bin/env bash
# 本地一键启动（需要本机已安装 PostgreSQL + pgvector + Redis + .NET 10 SDK + Node 20）

set -e

ROOT=$(cd "$(dirname "$0")" && pwd)
cd "$ROOT"

echo "==== 1. 启动 PostgreSQL (Docker) ===="
docker compose up -d postgres redis

echo "==== 2. 等待数据库就绪 ===="
for i in {1..30}; do
  if docker exec ai-cs-postgres pg_isready -U postgres >/dev/null 2>&1; then
    echo "  数据库已就绪"
    break
  fi
  sleep 1
done

echo "==== 3. 启动 .NET API ===="
cd backend/src/AiCustomerService.Api
ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS=http://+:5000 \
dotnet run &
API_PID=$!
sleep 8
echo "  API PID=$API_PID  http://localhost:5000"

echo "==== 4. 启动 Vue 后台 ===="
cd "$ROOT/frontend/admin"
npm install
npm run dev -- --host &
ADMIN_PID=$!
echo "  Admin PID=$ADMIN_PID  http://localhost:5173"

echo "==== 5. 启动 uni-app 工作台 (H5) ===="
cd "$ROOT/frontend/miniprogram"
npm install
npm run dev:h5 -- --host &
MP_PID=$!
echo "  MiniApp PID=$MP_PID  http://localhost:5174"

trap "kill $API_PID $ADMIN_PID $MP_PID 2>/dev/null; docker compose down" INT TERM EXIT
wait