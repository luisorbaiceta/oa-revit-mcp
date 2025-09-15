import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const PointSchema = z.object({
  x: z.number(),
  y: z.number(),
  z: z.number(),
});

const TransformSchema = z.object({
  basisX: PointSchema,
  basisY: PointSchema,
  basisZ: PointSchema,
  origin: PointSchema,
});

const BoundingBoxXYZSchema = z.object({
  min: PointSchema,
  max: PointSchema,
  transform: TransformSchema,
});

export function registerCreateDetailViewTool(server: McpServer) {
  server.tool(
    "create_detail_view",
    "Create a detail view in Revit.",
    {
      viewFamilyTypeId: z.number().describe("The ID of the view family type for the detail view."),
      sectionBox: BoundingBoxXYZSchema.describe("The section box for the detail view."),
    },
    async (args, extra) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand(
            "create_detail_view",
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
              text: `Create detail view failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
