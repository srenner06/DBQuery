#!/bin/bash

GIT_DIR=$(git rev-parse --git-dir)

echo "Removing git hooks..."
rm -f "$GIT_DIR"/hooks/pre-commit
rm -f "$GIT_DIR"/hooks/pre-push

echo "Git hooks removed!"
