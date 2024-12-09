# WSL2 Setup Guide

## Installing WSL2

1. **Open a PowerShell window with administrative privileges.**
2. **Run the following commands to enable WSL and Virtual Machine Platform features:**

    ```powershell
    dism.exe /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart
    dism.exe /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart

    # Restart your computer to apply changes
    Restart-Computer
    ```

## Updating WSL2 Kernel and Installing Ubuntu 24.04

1. **After restarting, open PowerShell.**
2. **Update the WSL kernel:**

    ```powershell
    wsl --update
    ```

3. **Set WSL2 as the default version:**

    ```powershell
    wsl --set-default-version 2
    ```

4. **Install Ubuntu 24.04:**

    ```powershell
    wsl --install -d Ubuntu-24.04
    ```

5. **Update Ubuntu packages after installation:**

    ```bash
    sudo apt update && sudo apt full-upgrade -y
    ```

## Setting Up WSL2 GUI (WSLg Links)

For more information about the WSLg Links project, visit [viruscamp WSLg Links](https://github.com/viruscamp/wslg-links).

1. **Install WSLg Links:**

    ```bash
    # Using unzip (if installed)
    # sudo apt install unzip
    wget https://github.com/viruscamp/wslg-links/archive/refs/heads/main.zip
    unzip main.zip && cd wslg-links-main

    # Using git (if installed)
    # sudo apt install git
    git clone https://github.com/viruscamp/wslg-links.git
    cd wslg-links

    sudo cp wslg-tmp-x11.service /usr/lib/systemd/system/
    sudo cp wslg-runtime-dir.service /usr/lib/systemd/user/
    sudo systemctl enable wslg-tmp-x11
    sudo systemctl --global enable wslg-runtime-dir
    exit
    ```

2. **Shutdown WSL:**

    ```powershell
    wsl --shutdown
    ```

## Configuring WSL2 GUI Environment Variables

1. **Open the Ubuntu terminal in WSL2:**

    ```powershell
    wsl -d Ubuntu-24.04 --cd ~
    ```

### CPU Rendering (Faster but Uses CPU Only)

```bash
export LIBGL_ALWAYS_SOFTWARE=true
export GALLIUM_DRIVER=llvmpipe
# Alternative:
# export GALLIUM_DRIVER=softpipe
```

### GPU Rendering (Requires Compatible Hardware)

Follow the [Microsoft WSL2 GUI setup documentation](https://learn.microsoft.com/en-us/windows/wsl/tutorials/gui-apps#prerequisites) for prerequisites. (Installed driver for vGPU)

#### For Intel GPUs

```bash
export GALLIUM_DRIVER=d3d12
export MESA_D3D12_DEFAULT_ADAPTER_NAME=INTEL
```

#### For NVIDIA GPUs

```bash
export GALLIUM_DRIVER=d3d12
export MESA_D3D12_DEFAULT_ADAPTER_NAME=NVIDIA
```

#### For AMD GPUs (Not Tested)

```bash
export GALLIUM_DRIVER=d3d12
export MESA_D3D12_DEFAULT_ADAPTER_NAME=AMD
```

---

This guide provides a comprehensive setup for WSL2 with Ubuntu 24.04, including optional GUI configurations. Ensure all dependencies are installed as needed.
