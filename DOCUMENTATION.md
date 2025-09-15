# Revit-MCP Documentation

## Introduction

Revit-MCP is a powerful tool that connects Large Language Models (LLMs) with Autodesk Revit, enabling AI-driven BIM automation. It allows you to interact with Revit using natural language commands, making it easier to perform complex tasks and automate repetitive workflows.

The project consists of three main components:

- **revit-mcp-server:** The server-side component that provides the tools and functionalities to the AI. It acts as a bridge between the LLM and the Revit plugin.
- **revit-mcp-plugin:** A Revit plugin that receives messages from the server, loads command sets, and operates on the Revit application.
- **revit-mcp-commandset:** A set of core commands for performing CRUD (Create, Read, Update, Delete) operations on Revit elements. You can also create your own custom commands to extend the functionality.

## Installation

To get started with Revit-MCP, you need to set up the server, the plugin, and the command set. Follow these steps to install and configure the entire project.

### 1. Environment Requirements

- **Revit:** 2019-2024
- **Node.js:** v18 or later

### 2. Set Up the Server

The server is responsible for connecting the AI to the Revit plugin. Here's how to set it up:

1.  **Clone the repository and navigate to the server directory:**
    ```bash
    git clone https://github.com/revit-mcp/revit-mcp.git
    cd revit-mcp/server
    ```

2.  **Install dependencies:**
    ```bash
    npm install
    ```

3.  **Build the server:**
    ```bash
    npm run build
    ```

4.  **Configure the client:**
    - Open your MCP-supported client (e.g., Claude).
    - Go to **Settings > Developer > Edit Config**.
    - Add the following configuration to `claude_desktop_config.json`:
      ```json
      {
          "mcpServers": {
              "revit-mcp": {
                  "command": "node",
                  "args": ["<path_to_your_project>/revit-mcp/server/build/index.js"]
              }
          }
      }
      ```
    - Replace `<path_to_your_project>` with the actual path to the `revit-mcp` project on your machine.
    - Restart the client. You should see a hammer icon, which indicates a successful connection to the MCP service.

### 3. Set Up the Revit Plugin

The Revit plugin is responsible for executing the commands sent by the server. Here's how to set it up:

1.  **Compile the plugin:**
    - Open the `plugin/revit-mcp-plugin.sln` solution in Visual Studio.
    - Build the solution to generate the `revit-mcp-plugin.dll` file.

2.  **Register the plugin:**
    - Create an `.addin` file in the Revit Addins folder (`C:\Users\[USERNAME]\AppData\Roaming\Autodesk\Revit\Addins\20XX`).
    - The content of the `.addin` file should be:
      ```xml
      <?xml version="1.0" encoding="utf-8"?>
      <RevitAddIns>
        <AddIn Type="Application">
          <Name>revit-mcp</Name>
          <Assembly>%your_path%\revit-mcp-plugin.dll</Assembly>
          <FullClassName>revit_mcp_plugin.Core.Application</FullClassName>
          <ClientId>090A4C8C-61DC-426D-87DF-E4BAE0F80EC1</ClientId>
          <VendorId>revit-mcp</VendorId>
          <VendorDescription>https://github.com/revit-mcp/revit-mcp-plugin</VendorDescription>
        </AddIn>
      </RevitAddIns>
      ```
    - Replace `%your_path%` with the actual path to the compiled `revit-mcp-plugin.dll`.

3.  **Restart Revit.**

### 4. Set Up the Command Set

The command set contains the commands that can be executed in Revit. Here's how to set it up:

1.  **Compile the command set:**
    - Open the `commandset/revit-mcp-commandset.sln` solution in Visual Studio.
    - Build the solution to generate the compiled output.

2.  **Create the command set folder:**
    - Go to the Revit Addins folder (`C:\Users\[USERNAME]\AppData\Roaming\Autodesk\Revit\Addins\20XX`).
    - Create a new folder named `RevitMCPCommandSet`.

3.  **Copy the files:**
    - Copy the `command.json` file from the `commandset` directory to the `RevitMCPCommandSet` folder.
    - Create a subfolder with the corresponding Revit version (e.g., `2023`).
    - Place the compiled output from the `revit-mcp-commandset` project into this subfolder.

4.  **Configure commands in Revit:**
    - In Revit, go to **Add-ins > Revit MCP Plugin > Settings**.
    - Click **OpenCommandSetFolder** to open the folder where the command sets are stored.
    - Check the commands you want to load and use.

5.  **Enable the service:**
    - Go to **Add-ins > Revit MCP Plugin > Revit MCP Switch**.
    - Turn on the service to allow the AI to connect to your Revit application.

## Usage

Once you have completed the installation and configuration, you can start using Revit-MCP to interact with Revit. Open your MCP-supported client and start sending commands. For example, you can ask the AI to:

- "Get all the walls in the current view."
- "Create a new door in the selected wall."
- "Delete the selected elements."

The AI will use the available tools to execute your commands in Revit.

## Custom Commands

You can extend the functionality of Revit-MCP by creating your own custom commands. To do this, you can refer to the `revit-mcp-commandset` project as a template.

When creating custom commands, make sure that the command names are identical between the `revit-mcp` server and the `revit-mcp-commandset` project. This will ensure that the AI can find and execute your custom commands correctly.

## Supported Tools

Here is a list of the supported tools that you can use with Revit-MCP:

| Name                        | Description                                          |
| --------------------------- | ---------------------------------------------------- |
| `get_current_view_info`     | Get information about the current view.              |
| `get_current_view_elements` | Get all elements in the current view.                |
| `get_available_family_types`| Get available family types in the current project.   |
| `get_selected_elements`     | Get the elements that are currently selected.        |
| `create_point_based_element`| Create a point-based element (e.g., door, window).   |
| `create_line_based_element` | Create a line-based element (e.g., wall, beam).      |
| `create_surface_based_element`| Create a surface-based element (e.g., floor).      |
| `delete_elements`           | Delete one or more elements.                         |
| `reset_model`               | Reset the model.                                     |
| `modify_element`            | Modify the properties of an element.                 |
| `search_modules`            | Search for available modules.                        |
| `use_module`                | Use a specific module.                               |
| `send_code_to_revit`        | Send a code snippet to be executed in Revit.         |
| `color_splash`              | Color elements based on a parameter value.           |
| `tag_walls`                 | Tag all walls in the current view.                   |
