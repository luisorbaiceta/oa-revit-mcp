import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateWallTool(server: McpServer) {
  server.tool(
    "create_wall",
    "Create a wall in Revit.",
    {
      startX: z.number().describe("The starting X coordinate of the wall."),
      startY: z.number().describe("The starting Y coordinate of the wall."),
      endX: z.number().describe("The ending X coordinate of the wall."),
      endY: z.number().describe("The ending Y coordinate of the wall."),
      height: z.number().describe("The height of the wall."),
      thickness: z.number().describe("The thickness of the wall."),
    },
    async (args, extra) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("createWall", args);
        });

        if (response.errorMessage && response.errorMessage.trim() !== "") {
          return {
            content: [
              {
                type: "text",
                text: `Create wall failed: ${response.errorMessage}`,
              },
            ],
          };
        }

        return {
          content: [
            {
              type: "text",
              text: `Wall created successfully!\nWall ID: ${response.elementId}\nStart Point: (${response.startPoint.x}, ${response.startPoint.y})\nEnd Point: (${response.endPoint.x}, ${response.endPoint.y})\nHeight: ${response.height}\nThickness: ${response.thickness}`,
            },
          ],
        };
      } catch (error) {
        return {
          content: [
            {
              type: "text",
              text: `Create wall failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
