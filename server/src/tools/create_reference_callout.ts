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

export function registerCreateReferenceCalloutTool(server: McpServer) {
  server.tool(
    "create_reference_callout",
    "Create a reference callout in Revit.",
    {
      parentViewId: z.number().describe("The ID of the parent view for the reference callout."),
      viewFamilyTypeId: z.number().describe("The ID of the view family type for the reference callout."),
      sectionBox: BoundingBoxXYZSchema.describe("The section box for the reference callout."),
    },
    async (args, extra) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand(
            "create_reference_callout",
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
              text: `Create reference callout failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
