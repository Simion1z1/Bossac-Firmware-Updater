# Welcome to the Bossac Firmware Updater
This tool is designed to streamline the process of automatically uploading firmware stored on GitHub to SAM/SAMD21 microcontrollers. It checks for new firmware versions and, if available, uploads them to the board seamlessly.

## How to Use
# 1. Clone the Repository:
Start by cloning this repository to your local environment.

### 2. Create Your Own Repository:
In your repository, include two key files:
'firmware.bin': This should be the compiled binary file from your build process.
'firmware_version.txt': This file should contain the firmware version in the following format: version=1.0.0.
Each time you update the 'firmware.bin' file, remember to increment the version number in this file.

### 3. Update the Code with Your Repository URLs:
In the program code, replace the example URLs with the links to your repository’s raw files. For example:

'versionFileUrl = "https://raw.githubusercontent.com/your_username/your_repo/your_branch/firmware_version.txt" '
'firmwareUrl = "https://raw.githubusercontent.com/your_username/your_repo/your_branch/firmware.bin" '
### 4. (Optional) Customize Program Paths:
You can modify the program paths and settings according to your preferences.

### 5. Connect Your SAM Board:
Ensure your SAM/SAMD21 board is connected to your computer.

### 6. Run the Program:
Execute the firmware updater.

### 7. Enter Bootloader Mode:
For SAMD21 boards, simply double-press the reset button to enter bootloader mode. The program will automatically recognize the port and upload the firmware.

### Note: If you don’t already have BOSSA installed, it will be automatically installed in '"C:\Program Files\BOSSA\bossac.exe"'.

Thank you for using this tool to manage your firmware updates!
