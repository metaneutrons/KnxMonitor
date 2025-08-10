class Knxmonitor < Formula
  desc "Enterprise-grade KNX/EIB bus monitoring and debugging tool"
  homepage "https://github.com/metaneutrons/KnxMonitor"
  url "https://github.com/metaneutrons/KnxMonitor/archive/refs/tags/v1.0.0.tar.gz"
  sha256 "0000000000000000000000000000000000000000000000000000000000000000" # Will be updated with actual SHA
  license "GPL-3.0-or-later"
  head "https://github.com/metaneutrons/KnxMonitor.git", branch: "main"

  depends_on "dotnet" => :build

  def install
    # Build the application
    system "dotnet", "publish", "KnxMonitor/KnxMonitor.csproj",
           "--configuration", "Release",
           "--runtime", OS.mac? ? "osx-#{Hardware::CPU.arch}" : "linux-#{Hardware::CPU.arch}",
           "--self-contained", "true",
           "--output", "publish",
           "-p:PublishTrimmed=true",
           "-p:PublishSingleFile=true"

    # Install the binary
    bin.install "publish/KnxMonitor" => "knxmonitor"

    # Install documentation
    prefix.install_metafiles
    
    # Install man page if it exists
    if File.exist?("docs/knxmonitor.1")
      man1.install "docs/knxmonitor.1"
    end

    # Install configuration examples
    if Dir.exist?("examples")
      (share/"knxmonitor").install Dir["examples/*"]
    end
  end

  service do
    run [opt_bin/"knxmonitor", "--daemon"]
    keep_alive false
    log_path var/"log/knxmonitor.log"
    error_log_path var/"log/knxmonitor.error.log"
    environment_variables PATH: std_service_path_env
  end

  test do
    # Test version output
    assert_match version.to_s, shell_output("#{bin}/knxmonitor --version")
    
    # Test help output
    help_output = shell_output("#{bin}/knxmonitor --help")
    assert_match "KNX Monitor", help_output
    assert_match "Enterprise-grade", help_output
    
    # Test configuration validation
    system bin/"knxmonitor", "--validate-config"
    
    # Test health check functionality
    begin
      pid = spawn(bin/"knxmonitor", "--health-check-port", "18080", "--daemon")
      sleep 2
      
      # Check if health endpoint responds
      system "curl", "-f", "http://localhost:18080/health"
      assert $?.success?, "Health check endpoint should respond"
    ensure
      Process.kill("TERM", pid) if pid
      Process.wait(pid) if pid
    end
  end
end
