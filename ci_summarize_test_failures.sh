#!/bin/bash
# Appends a test failure summary to the workflow run's summary page ($GITHUB_STEP_SUMMARY):
# the failed test names from the trx results, any crash dumps produced, and the tests that
# were in-flight when the test host crashed or hung (from the vstest blame collector).
# Run from the repository root in an 'if: failure()' step after the 'dotnet test' step.

mkdir -p TestResults  # may not exist if the build failed before the test step

FAILED_TESTS=$(grep -rhoE '<UnitTestResult[^>]*outcome="Failed"[^>]*>' TestResults --include='*.trx' 2>/dev/null \
  | grep -oE 'testName="[^"]*"' | sed 's/testName=//;s/"//g' | sort -u)

{
  echo "## Test failures"
  echo '```'
  if [ -n "$FAILED_TESTS" ]; then
    echo "$FAILED_TESTS"
  else
    echo "no failed tests recorded in trx"
  fi
  echo '```'

  DUMPS=$(find TestResults -name '*.dmp' -o -name '*.crashreport.json' 2>/dev/null)
  if [ -n "$DUMPS" ]; then
    echo "## Crash dumps (see run artifacts)"
    echo '```'
    ls -lh $DUMPS
    echo '```'
  fi

  # Tests that were in-flight when the host crashed or hung (from the blame collector)
  SEQ=$(find TestResults -name 'Sequence_*.xml' 2>/dev/null)
  if [ -n "$SEQ" ]; then
    echo "## Tests in progress at crash/hang (blame sequence)"
    echo '```'
    cat $SEQ
    echo  # the sequence xml may not end with a newline
    echo '```'
  fi
} >> "$GITHUB_STEP_SUMMARY"
