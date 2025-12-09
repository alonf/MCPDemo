#!/bin/bash
# Setup Claude Desktop with WinDiag MCP Server (macOS/Linux)
# This script installs (if needed) and configures Claude Desktop to use the WinDiag MCP Server

set -e

echo "============================================"
echo "Claude Desktop MCP Configuration"
echo "============================================"
echo ""

# Get the current project directory
PROJECT_ROOT="$(cd "$(dirname "$0")" && pwd)"
PROJECT_PATH="$PROJECT_ROOT/WinDiagMcpServer/WinDiagMcpServer.csproj"

echo "Project Root: $PROJECT_ROOT"
echo "Project Path: $PROJECT_PATH"
echo ""

# Determine OS and config path
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS
    CLAUDE_CONFIG_DIR="$HOME/Library/Application Support/Claude"
    PLATFORM="macOS"
elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
    # Linux
    CLAUDE_CONFIG_DIR="$HOME/.config/Claude"
    PLATFORM="Linux"
else
    echo "Unsupported operating system: $OSTYPE"
    exit 1
fi

CLAUDE_CONFIG_FILE="$CLAUDE_CONFIG_DIR/claude_desktop_config.json"

echo "[1/5] Checking Claude Desktop installation..."

if [ ! -d "$CLAUDE_CONFIG_DIR" ]; then
    echo "      Claude Desktop not found!"
    echo ""
    
    # macOS: Try Homebrew installation
    if [[ "$PLATFORM" == "macOS" ]]; then
        if command -v brew &> /dev/null; then
            echo "      Attempting to install Claude Desktop using Homebrew..."
            echo ""
            
            if brew install --cask claude; then
                echo "      Claude Desktop installed successfully!"
                echo "      Please restart this script to configure it."
                echo ""
                exit 0
            else
                echo "      Automated installation failed."
                echo ""
                echo "Please install Claude Desktop manually:"
                echo "  Option 1 (Recommended):"
                echo "    brew install --cask claude"
                echo ""
                echo "  Option 2: Download from"
                echo "    https://claude.ai/download"
                echo ""
                echo "Then run this script again."
                echo ""
                exit 1
            fi
        else
            echo "      Homebrew not available. Install manually:"
            echo ""
            echo "  Option 1: Install Homebrew first"
            echo "    /bin/bash -c \"\$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)\""
            echo "    Then run: brew install --cask claude"
            echo ""
            echo "  Option 2: Download Claude Desktop directly"
            echo "    https://claude.ai/download"
            echo ""
            echo "Then run this script again."
            echo ""
            exit 1
        fi
    fi
    
    # Linux: Manual installation required
    if [[ "$PLATFORM" == "Linux" ]]; then
        echo "      Please install Claude Desktop manually:"
        echo ""
        echo "  Visit: https://claude.ai/download"
        echo ""
        echo "  Or use your distribution's package manager if available."
        echo ""
        echo "Then run this script again."
        echo ""
        exit 1
    fi
fi

echo "      Claude Desktop found at: $CLAUDE_CONFIG_DIR"
echo ""

# Build the server first
echo "[2/5] Building WinDiag MCP Server..."
if ! dotnet build --nologo --verbosity quiet; then
    echo "      Build failed"
    exit 1
fi
echo "      Build successful!"
echo ""

# Create or update Claude Desktop configuration
echo "[3/5] Configuring Claude Desktop..."

# Create config directory if it doesn't exist
mkdir -p "$CLAUDE_CONFIG_DIR"

# Create backup of existing config
if [ -f "$CLAUDE_CONFIG_FILE" ]; then
    BACKUP_FILE="$CLAUDE_CONFIG_FILE.backup-$(date +%Y%m%d-%H%M%S)"
    cp "$CLAUDE_CONFIG_FILE" "$BACKUP_FILE"
    echo "      Backed up existing config to: $BACKUP_FILE"
fi

# Create configuration JSON
cat > "$CLAUDE_CONFIG_FILE" << EOF
{
  "mcpServers": {
    "windiag": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "$PROJECT_PATH"
      ]
    }
  }
}
EOF

echo "      Configuration written successfully!"
echo ""

# Display configuration
echo "[4/5] Configuration Summary:"
echo ""
echo "  Config File: $CLAUDE_CONFIG_FILE"
echo ""
echo "  MCP Server Configuration:"
cat "$CLAUDE_CONFIG_FILE"
echo ""

# Check if Claude Desktop is running
echo "[5/5] Checking Claude Desktop status..."
if pgrep -x "Claude" > /dev/null; then
    echo "      Claude Desktop is running"
    echo "      You need to restart it for changes to take effect"
else
    echo "      Claude Desktop is not currently running"
fi
echo ""

echo "============================================"
echo "Setup Complete! ✓"
echo "============================================"
echo ""

echo "Next Steps:"
echo ""
echo "  1. Restart Claude Desktop"
echo "     (Close and reopen the application)"
echo ""
echo "  2. Start a new conversation in Claude"
echo ""
echo "  3. Ask Claude:"
echo "     'What tools do you have access to?'"
echo ""
echo "  4. Test the tool:"
echo "     'What is my system information?'"
echo ""
echo "  5. Claude should use the get_system_info tool"
echo "     and display your system diagnostics!"
echo ""

echo "Troubleshooting:"
echo ""
echo "  If tools don't appear:"
echo "    - Make sure Claude Desktop is fully closed and restarted"
echo "    - Check that .NET SDK is in your PATH"
echo "    - Look for errors in Claude's developer console"
echo ""
echo "  For more help, see: docs/MCP_TESTING_GUIDE.md"
echo ""
