import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateAreaPlanTool(server: McpServer) {
  server.tool(
    "create_area_plan",
    "Create an area plan in Revit.",
    {
      areaSchemeId: z.number().describe("The ID of the area scheme for the area plan."),
      levelId: z.number().describe("The ID of the level for the area plan."),
    },
    async (args, extra) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand(
            "create_area_plan",
            args
          );
        });

        return {
          content: [
            {
              type: "text",
              text: JSON.stringify(response, null, 2),
            },
          ],
        };
      } catch (error) {
        return {
          content: [
            {
              type: "text",
              text: `Create area plan failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
