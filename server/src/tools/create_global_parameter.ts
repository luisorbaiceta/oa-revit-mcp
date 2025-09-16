import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateGlobalParameterTool(server: McpServer) {
  server.tool(
    "create_global_parameter",
    "Creates a new global parameter in the Revit model.",
    {
      name: z.string().describe("The name of the new global parameter."),
      type: z.string().describe("The type of the global parameter."),
      spec: z.string().describe("The spec of the global parameter."),
      isReporting: z.boolean().describe("Whether the parameter is a reporting parameter."),
    },
    async (args, extra) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_global_parameter", args);
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
              text: `Create global parameter failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
