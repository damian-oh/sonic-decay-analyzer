#!/usr/bin/env python3
"""
Persistent server mode for SonicDecay.Engine.

Provides low-latency analysis by maintaining a persistent Python process.
Eliminates ~500-1000ms startup overhead per analysis request.

Protocol:
    1. Server starts and prints "READY" to stdout
    2. Client sends JSON request per line to stdin
    3. Server processes and returns JSON response per line to stdout
    4. Client sends "EXIT" to gracefully terminate

Usage:
    python server.py

    Input line (JSON):
    {"samples": [...], "sample_rate": 48000, ...}

    Output line (JSON):
    {"spectral_centroid": 2800.5, "success": true, ...}
"""

import sys
import os
import json

# Add current directory to path for direct script execution
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from cli import process_analysis_request


def run_server() -> int:
    """
    Run the persistent analysis server.

    Reads requests line by line from stdin, processes them,
    and writes responses line by line to stdout.

    Returns:
        Exit code (0 for normal shutdown).
    """
    # Signal ready to parent process
    print("READY", flush=True)

    while True:
        try:
            # Read a line from stdin
            line = sys.stdin.readline()

            # EOF or empty read
            if not line:
                break

            # Strip whitespace
            line = line.strip()

            # Empty line - skip
            if not line:
                continue

            # Exit command
            if line.upper() == "EXIT":
                break

            # Parse JSON request
            try:
                request = json.loads(line)
            except json.JSONDecodeError as e:
                error_response = {
                    "success": False,
                    "error_message": f"Invalid JSON: {str(e)}",
                    "spectral_centroid": 0.0,
                    "hf_energy_ratio": 0.0,
                    "fundamental_freq": 0.0,
                    "fundamental_magnitude": 0.0,
                    "decay_percentage": None,
                    "sample_rate": 0,
                    "fft_size": 0,
                }
                print(json.dumps(error_response), flush=True)
                continue

            # Process the analysis request
            result = process_analysis_request(request)

            # Output JSON response (single line)
            print(json.dumps(result), flush=True)

        except KeyboardInterrupt:
            break
        except Exception as e:
            # Unexpected error - try to continue
            error_response = {
                "success": False,
                "error_message": f"Server error: {str(e)}",
                "spectral_centroid": 0.0,
                "hf_energy_ratio": 0.0,
                "fundamental_freq": 0.0,
                "fundamental_magnitude": 0.0,
                "decay_percentage": None,
                "sample_rate": 0,
                "fft_size": 0,
            }
            try:
                print(json.dumps(error_response), flush=True)
            except Exception:
                pass

    return 0


if __name__ == "__main__":
    sys.exit(run_server())
