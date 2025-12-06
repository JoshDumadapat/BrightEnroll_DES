#!/bin/bash

# Database Backup Script for BrightEnroll_DES
# Creates a SQL Server database dump with metadata (timestamp, git SHA)

set -e  # Exit on error

# Configuration - can be overridden by environment variables
DB_SERVER="${DB_SERVER:-localhost}"
DB_NAME="${DB_NAME:-DB_BrightEnroll_DES}"
DB_USER="${DB_USER:-}"
DB_PASSWORD="${DB_PASSWORD:-}"
USE_INTEGRATED_SECURITY="${USE_INTEGRATED_SECURITY:-true}"
BACKUP_DIR="${BACKUP_DIR:-./backups}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored messages
print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Get git SHA (if in a git repository)
get_git_sha() {
    if command -v git &> /dev/null && git rev-parse --git-dir > /dev/null 2>&1; then
        git rev-parse --short HEAD 2>/dev/null || echo "unknown"
    else
        echo "unknown"
    fi
}

# Get git branch (if in a git repository)
get_git_branch() {
    if command -v git &> /dev/null && git rev-parse --git-dir > /dev/null 2>&1; then
        git rev-parse --abbrev-ref HEAD 2>/dev/null || echo "unknown"
    else
        echo "unknown"
    fi
}

# Create backup directory if it doesn't exist
mkdir -p "$BACKUP_DIR"

# Generate timestamp
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
GIT_SHA=$(get_git_sha)
GIT_BRANCH=$(get_git_branch)

# Generate backup filename
BACKUP_FILE="${BACKUP_DIR}/DB_BrightEnroll_DES_${TIMESTAMP}.bak"
METADATA_FILE="${BACKUP_DIR}/DB_BrightEnroll_DES_${TIMESTAMP}.metadata.txt"

print_info "Starting database backup..."
print_info "Database: $DB_NAME"
print_info "Server: $DB_SERVER"
print_info "Backup file: $BACKUP_FILE"

# Build SQLCMD connection string
if [ "$USE_INTEGRATED_SECURITY" = "true" ]; then
    SQLCMD_CONNECTION="-S $DB_SERVER -E"
else
    if [ -z "$DB_USER" ] || [ -z "$DB_PASSWORD" ]; then
        print_error "DB_USER and DB_PASSWORD must be set when USE_INTEGRATED_SECURITY is false"
        exit 1
    fi
    SQLCMD_CONNECTION="-S $DB_SERVER -U $DB_USER -P $DB_PASSWORD"
fi

# Check if sqlcmd is available
if ! command -v sqlcmd &> /dev/null; then
    print_error "sqlcmd is not installed or not in PATH"
    print_info "On Windows, sqlcmd is typically available with SQL Server tools"
    print_info "On Linux/Mac, install mssql-tools package"
    exit 1
fi

# Create backup using sqlcmd
print_info "Creating database backup..."

# For SQL Server, we'll use BACKUP DATABASE command
BACKUP_SQL="BACKUP DATABASE [$DB_NAME] TO DISK = '$BACKUP_FILE' WITH FORMAT, INIT, COMPRESSION;"

if sqlcmd $SQLCMD_CONNECTION -Q "$BACKUP_SQL" -d master; then
    print_info "Backup created successfully: $BACKUP_FILE"
else
    print_error "Failed to create backup"
    exit 1
fi

# Create metadata file
print_info "Creating metadata file..."
cat > "$METADATA_FILE" << EOF
Database Backup Metadata
========================
Timestamp: $(date -u +"%Y-%m-%d %H:%M:%S UTC")
Local Time: $(date +"%Y-%m-%d %H:%M:%S %Z")
Database: $DB_NAME
Server: $DB_SERVER
Backup File: $(basename "$BACKUP_FILE")
Backup Size: $(du -h "$BACKUP_FILE" | cut -f1)
Git SHA: $GIT_SHA
Git Branch: $GIT_BRANCH
Created By: $(whoami)
Host: $(hostname)
EOF

print_info "Metadata saved to: $METADATA_FILE"

# Log entry
LOG_FILE="${BACKUP_DIR}/backup_log.txt"
LOG_ENTRY="[$(date +"%Y-%m-%d %H:%M:%S")] Backup created: $(basename "$BACKUP_FILE") | SHA: $GIT_SHA | Branch: $GIT_BRANCH | Size: $(du -h "$BACKUP_FILE" | cut -f1)"
echo "$LOG_ENTRY" >> "$LOG_FILE"

print_info "Log entry added to: $LOG_FILE"
print_info "Backup completed successfully!"
print_info "Files:"
print_info "  - Backup: $BACKUP_FILE"
print_info "  - Metadata: $METADATA_FILE"
print_info "  - Log: $LOG_FILE"

