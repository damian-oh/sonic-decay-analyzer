#!/usr/bin/env python3
"""
Command-line interface for SonicDecay.Engine.

Provides JSON-based interop for .NET MAUI frontend via process spawning.
Reads analysis requests from stdin, outputs results to stdout.

Usage:
    echo '{"samples": [...], "sample_rate": 48000}' | python cli.py analyze
    python cli.py analyze --file input.json
    python cli.py version

Protocol:
    Input (JSON):
    {
        "samples": [0.1, -0.2, ...],     # Normalized float samples
        "sample_rate": 48000,             # Sample rate in Hz
        "expected_fundamental": 82.41,    # Optional: expected f₀
        "initial_centroid": 3500.0,       # Optional: baseline for decay
        "fft_size": 8192,                 # Optional: FFT size
        "window_type": "hamming"          # Optional: window function
    }

    Output (JSON):
    {
        "spectral_centroid": 2800.5,
        "hf_energy_ratio": 0.45,
        "fundamental_freq": 82.3,
        "fundamental_magnitude": 0.85,
        "decay_percentage": 20.0,
        "sample_rate": 48000,
        "fft_size": 8192,
        "success": true,
        "error_message": null
    }
"""

import sys
import os
import json
import argparse
import numpy as np
from typing import Optional

# Add current directory to path for direct script execution
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from analysis import analyze_audio_buffer, WindowType, AnalysisResult


def parse_window_type(window_str: Optional[str]) -> WindowType:
    """Convert string to WindowType enum."""
    if window_str is None:
        return WindowType.HAMMING

    mapping = {
        "hamming": WindowType.HAMMING,
        "hann": WindowType.HANN,
        "hanning": WindowType.HANN,
        "blackman": WindowType.BLACKMAN,
        "rectangular": WindowType.RECTANGULAR,
        "rect": WindowType.RECTANGULAR,
    }

    return mapping.get(window_str.lower(), WindowType.HAMMING)


def process_analysis_request(request: dict) -> dict:
    """
    Process a single analysis request and return results.

    Args:
        request: Dictionary containing analysis parameters.

    Returns:
        Dictionary containing analysis results.
    """
    try:
        # Extract required parameters
        samples = request.get("samples")
        sample_rate = request.get("sample_rate", 48000)

        if samples is None:
            return {
                "success": False,
                "error_message": "Missing required field: samples",
                "spectral_centroid": 0.0,
                "hf_energy_ratio": 0.0,
                "fundamental_freq": 0.0,
                "fundamental_magnitude": 0.0,
                "decay_percentage": None,
                "sample_rate": sample_rate,
                "fft_size": 0,
            }

        # Convert samples to numpy array
        samples_array = np.array(samples, dtype=np.float64)

        # Extract optional parameters
        expected_fundamental = request.get("expected_fundamental")
        initial_centroid = request.get("initial_centroid")
        fft_size = request.get("fft_size", 8192)
        window_type = parse_window_type(request.get("window_type"))
        hf_low = request.get("hf_low", 5000.0)
        hf_high = request.get("hf_high", 15000.0)

        # Run analysis
        result = analyze_audio_buffer(
            samples=samples_array,
            sample_rate=sample_rate,
            expected_fundamental=expected_fundamental,
            initial_centroid=initial_centroid,
            fft_size=fft_size,
            window_type=window_type,
            hf_low=hf_low,
            hf_high=hf_high,
        )

        return result.to_dict()

    except Exception as e:
        return {
            "success": False,
            "error_message": f"Processing error: {str(e)}",
            "spectral_centroid": 0.0,
            "hf_energy_ratio": 0.0,
            "fundamental_freq": 0.0,
            "fundamental_magnitude": 0.0,
            "decay_percentage": None,
            "sample_rate": request.get("sample_rate", 0),
            "fft_size": 0,
        }


def cmd_analyze(args: argparse.Namespace) -> int:
    """Handle the analyze command."""
    try:
        # Read input
        if args.file:
            with open(args.file, "r") as f:
                input_data = f.read()
        else:
            input_data = sys.stdin.read()

        if not input_data.strip():
            error_result = {
                "success": False,
                "error_message": "No input data provided",
            }
            print(json.dumps(error_result))
            return 1

        # Parse JSON request
        request = json.loads(input_data)

        # Process request
        result = process_analysis_request(request)

        # Output JSON result
        print(json.dumps(result))

        return 0 if result.get("success", False) else 1

    except json.JSONDecodeError as e:
        error_result = {
            "success": False,
            "error_message": f"Invalid JSON input: {str(e)}",
        }
        print(json.dumps(error_result))
        return 1

    except Exception as e:
        error_result = {
            "success": False,
            "error_message": f"Unexpected error: {str(e)}",
        }
        print(json.dumps(error_result))
        return 1


def cmd_version(args: argparse.Namespace) -> int:
    """Handle the version command."""
    print(json.dumps({
        "version": "0.1.0",
        "success": True,
    }))
    return 0


def cmd_test(args: argparse.Namespace) -> int:
    """Run a self-test with synthetic signal."""
    from analysis import generate_test_signal

    # Generate test signal: 82.41 Hz (Low E) with harmonics
    samples = generate_test_signal(
        frequency=82.41,
        sample_rate=48000,
        duration=0.1,
        harmonics=[(2, 0.5), (3, 0.3), (4, 0.2), (5, 0.1)]
    )

    request = {
        "samples": samples.tolist(),
        "sample_rate": 48000,
        "expected_fundamental": 82.41,
    }

    result = process_analysis_request(request)

    print(json.dumps(result, indent=2))

    if result.get("success"):
        print(f"\nSelf-test PASSED", file=sys.stderr)
        print(f"  Fundamental: {result['fundamental_freq']:.2f} Hz", file=sys.stderr)
        print(f"  Centroid: {result['spectral_centroid']:.2f} Hz", file=sys.stderr)
        print(f"  HF Ratio: {result['hf_energy_ratio']:.4f}", file=sys.stderr)
        return 0
    else:
        print(f"\nSelf-test FAILED: {result.get('error_message')}", file=sys.stderr)
        return 1


def main() -> int:
    """Main entry point for CLI."""
    parser = argparse.ArgumentParser(
        prog="SonicDecay.Engine",
        description="Spectral analysis engine for acoustic degradation measurement",
    )

    subparsers = parser.add_subparsers(dest="command", help="Available commands")

    # Analyze command
    analyze_parser = subparsers.add_parser(
        "analyze",
        help="Analyze audio buffer and output spectral metrics"
    )
    analyze_parser.add_argument(
        "--file", "-f",
        help="Read input from file instead of stdin"
    )
    analyze_parser.set_defaults(func=cmd_analyze)

    # Version command
    version_parser = subparsers.add_parser(
        "version",
        help="Print version information"
    )
    version_parser.set_defaults(func=cmd_version)

    # Test command
    test_parser = subparsers.add_parser(
        "test",
        help="Run self-test with synthetic signal"
    )
    test_parser.set_defaults(func=cmd_test)

    args = parser.parse_args()

    if args.command is None:
        parser.print_help()
        return 1

    return args.func(args)


if __name__ == "__main__":
    sys.exit(main())
