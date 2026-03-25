#!/bin/sh
set -e

# Ensure data directories exist
mkdir -p /app/data/eventstore
mkdir -p /app/data/snapshots
mkdir -p /app/data/projections

# Set proper permissions
chown -R appuser:appuser /app/data

# Run the application
exec "$@"
