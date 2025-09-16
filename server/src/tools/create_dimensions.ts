import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const JZPointSchema = z.object({
  x: z.number(),
  y: z.number(),
  z: z.number(),
});

const DimensionCreationInfoSchema = z.object({
  startPoint: JZPointSchema,
  endPoint: JZPointSchema,
  linePoint: JZPointSchema,
  elementIds: z.array(z.number()),
  dimensionType: z.string().optional(),
  dimensionStyleId: z.number().optional(),
  viewId: z.number().optional(),
  options: z.record(z.any()).optional(),
});

export function registerCreateDimensionsTool(server: McpServer) {
  server.tool(
    "create_dimensions",
    "Create dimensions in a Revit view.",
    {
      dimensions: z.array(DimensionCreationInfoSchema),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_dimensions", params);
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
              text: `Dimension creation failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
