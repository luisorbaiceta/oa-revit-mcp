import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerUpdateElementParameterTool(server: McpServer) {
  server.tool(
    "update_element_parameter",
    "Updates the value of a specified parameter for a given element.",
    {
      elementId: z.number().describe("The ID of the element to update."),
      parameterName: z.string().describe("The name of the parameter to update."),
      parameterValue: z
        .any()
        .describe("The new value for the parameter."),
    },
    async (args, extra) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("update_element_parameter", args);
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
              text: `Update element parameter failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
