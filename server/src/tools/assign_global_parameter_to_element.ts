import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerAssignGlobalParameterToElementTool(server: McpServer) {
  server.tool(
    "assign_global_parameter_to_element",
    "Associates a parameter of an element with a global parameter.",
    {
      elementId: z.number().describe("The ID of the element."),
      parameterName: z
        .string()
        .describe("The name of the parameter on the element."),
      globalParameterId: z
        .number()
        .describe("The ID of the global parameter to associate with."),
    },
    async (args, extra) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("assign_global_parameter_to_element", args);
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
              text: `Assign global parameter to element failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
