#!/bin/bash
# Start the AlsaSharp Console (Consolonia) application

cd "$(dirname "$0")/src/AlsaSharp.Console.Consolonia"

echo "Starting AlsaSharp Console"
echo "Make sure the API is running at http://localhost:5000"
echo "Press Ctrl+C to exit"
echo ""

dotnet run --no-build
