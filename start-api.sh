#!/bin/bash
# Start the AlsaSharp API server

cd "$(dirname "$0")/src/AlsaSharp.Api"

echo "Starting AlsaSharp API on http://localhost:5000"
echo "Press Ctrl+C to stop the server"
echo ""

dotnet run --no-build
