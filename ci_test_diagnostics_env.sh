#!/bin/bash
# Enables crash diagnostics for the dotnet test host. Source this (do not execute it)
# in the CI step that runs 'dotnet test' so the exports apply to the test process:
#
#   . ./ci_test_diagnostics_env.sh
#
# On a test host crash, the runtime writes a minidump plus a crash report json
# (native + managed stacks of every thread) into TestResults so the artifact upload
# step preserves them. This also catches crashes the vstest blame collector misses.
export DOTNET_DbgEnableMiniDump=1
export DOTNET_DbgMiniDumpType=1  # mini: full dumps of a test host with CPython loaded are too large
export DOTNET_DbgMiniDumpName="$GITHUB_WORKSPACE/TestResults/coredump.%p.dmp"
export DOTNET_EnableCrashReport=1

# Print the Python tracebacks of all threads to stderr on a fatal signal (SIGSEGV/SIGABRT),
# so a native crash in the Python runtime is identifiable directly from the console log
export PYTHONFAULTHANDLER=1
