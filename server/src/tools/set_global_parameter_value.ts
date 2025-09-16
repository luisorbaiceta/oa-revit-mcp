import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerSetGlobalParameterValueTool(server: McpServer) {
  server.tool(
    "set_global_parameter_value",
    "Sets the value of a global parameter.",
    {
      id: z.number().describe("The ID of the global parameter."),
      value: z.any().describe("The new value for the global parameter."),
    },
    async (args, extra) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("set_global_parameter_value", args);
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
              text: `Set global parameter value failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
