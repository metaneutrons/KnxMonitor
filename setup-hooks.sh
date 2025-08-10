#!/bin/bash
# Setup git hooks for KNX Monitor

echo "Setting up git hooks for conventional commits..."

# Create commit-msg hook
cat > .git/hooks/commit-msg << 'EOF'
#!/bin/sh
# Conventional commit format validation

commit_msg_file="$1"
first_line=$(head -n1 "$commit_msg_file")

# Check conventional commit format
if ! echo "$first_line" | grep -qE "^(feat|fix|docs|style|refactor|test|chore|build|ci|perf|revert)(\([a-z0-9-]+\))?: .+"; then
    echo "❌ Commit message must follow conventional commit format"
    echo "Format: <type>(scope): <description>"
    echo "Types: feat, fix, docs, style, refactor, test, chore, build, ci, perf, revert"
    echo "Example: feat(monitoring): add KNX group address filtering"
    echo ""
    echo "Your commit message:"
    echo "$first_line"
    exit 1
fi

# Check for breaking changes
if echo "$first_line" | grep -q "!"; then
    echo "⚠️  Breaking change detected in commit message"
fi

echo "✅ Commit message follows conventional commit format"
EOF

# Create pre-commit hook
cat > .git/hooks/pre-commit << 'EOF'
#!/bin/sh
# Pre-commit hook for KNX Monitor

echo "🎨 Checking code formatting with CSharpier..."
if command -v dotnet >/dev/null 2>&1; then
    if dotnet tool list --local | grep -q csharpier; then
        dotnet csharpier check .
        
        if [ $? -ne 0 ]; then
            echo "❌ Code formatting issues found. Running formatter..."
            dotnet csharpier format .
            echo "✅ Code formatted. Please review and commit again."
            exit 1
        fi
    else
        echo "⚠️  CSharpier not installed. Run: dotnet tool restore"
    fi
else
    echo "⚠️  .NET CLI not found. Skipping formatting check."
fi

echo "🏗️ Building project..."
if command -v dotnet >/dev/null 2>&1; then
    dotnet build --verbosity quiet --configuration Debug
    
    if [ $? -ne 0 ]; then
        echo "❌ Build failed. Please fix errors before committing."
        exit 1
    fi
else
    echo "⚠️  .NET CLI not found. Skipping build check."
fi

echo "✅ Pre-commit checks passed!"
EOF

# Create pre-push hook
cat > .git/hooks/pre-push << 'EOF'
#!/bin/sh
# Pre-push hook for KNX Monitor

echo "🧪 Running tests..."
if command -v dotnet >/dev/null 2>&1; then
    dotnet test --verbosity quiet --configuration Release
    
    if [ $? -ne 0 ]; then
        echo "❌ Tests failed. Please fix failing tests before pushing."
        exit 1
    fi
else
    echo "⚠️  .NET CLI not found. Skipping test execution."
fi

echo "✅ Pre-push checks passed!"
EOF

# Create prepare-commit-msg hook for conventional commit templates
cat > .git/hooks/prepare-commit-msg << 'EOF'
#!/bin/sh
# Prepare commit message with conventional commit template

commit_msg_file="$1"
commit_source="$2"

# Only add template for regular commits (not merges, amends, etc.)
if [ "$commit_source" = "" ] || [ "$commit_source" = "template" ]; then
    # Check if the commit message is empty or only contains comments
    if ! grep -qv "^#" "$commit_msg_file" 2>/dev/null || [ ! -s "$commit_msg_file" ]; then
        cat > "$commit_msg_file" << 'TEMPLATE'
# <type>(scope): <description>
#
# Types:
#   feat:     A new feature
#   fix:      A bug fix
#   docs:     Documentation only changes
#   style:    Changes that do not affect the meaning of the code
#   refactor: A code change that neither fixes a bug nor adds a feature
#   test:     Adding missing tests or correcting existing tests
#   chore:    Changes to the build process or auxiliary tools
#   build:    Changes that affect the build system or external dependencies
#   ci:       Changes to CI configuration files and scripts
#   perf:     A code change that improves performance
#   revert:   Reverts a previous commit
#
# Scopes (examples):
#   monitoring: KNX bus monitoring functionality
#   connection: Connection handling (tunneling, routing, USB)
#   csv:        CSV group address database
#   docker:     Docker-related changes
#   cli:        Command-line interface
#   health:     Health check functionality
#   logging:    Logging and output formatting
#   config:     Configuration handling
#
# Examples:
#   feat(monitoring): add real-time KNX bus traffic visualization
#   fix(connection): resolve IP tunneling timeout issues
#   docs(readme): update installation instructions
#   chore(deps): update .NET to version 9.0.1
#
# Breaking changes: Add ! after type/scope
#   feat(cli)!: change default connection type to routing
TEMPLATE
    fi
fi
EOF

# Make hooks executable
chmod +x .git/hooks/commit-msg .git/hooks/pre-commit .git/hooks/pre-push .git/hooks/prepare-commit-msg

echo "✅ Git hooks installed successfully!"
echo ""
echo "📋 Conventional commit format:"
echo "  <type>(scope): <description>"
echo ""
echo "🏷️  Types: feat, fix, docs, style, refactor, test, chore, build, ci, perf, revert"
echo "📦 Example scopes: monitoring, connection, csv, docker, cli, health, logging, config"
echo "💡 Example: feat(monitoring): add KNX group address filtering"
echo ""
echo "🔧 Hooks installed:"
echo "  • commit-msg: Validates conventional commit format"
echo "  • pre-commit: Formats code and builds project"
echo "  • pre-push: Runs tests before pushing"
echo "  • prepare-commit-msg: Provides commit message template"
echo ""
echo "⚙️  To install required tools, run:"
echo "  dotnet tool restore"
