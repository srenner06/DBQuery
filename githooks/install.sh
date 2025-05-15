#!/bin/bash

HOOKS_DIR=$(dirname "$0")
GIT_DIR=$(git rev-parse --git-dir)

echo "Installing git hooks..."
cp "$HOOKS_DIR"/pre-* "$GIT_DIR"/hooks/
chmod +x "$GIT_DIR"/hooks/pre-*

echo "Git hooks installed!"
