import { z } from "zod";
import { tool } from "@modelcontext/tool-runtime";
import {
  executeCommand,
  Command,
} from "../utils/executeCommand";

const CreateGlobalParameterSchema = z.object({
  name: z.string().describe("The name of the new global parameter."),
  type: z.string().describe("The type of the global parameter."),
  spec: z.string().describe("The spec of the global parameter."),
  isReporting: z.boolean().describe("Whether the parameter is a reporting parameter."),
});

async function createGlobalParameter(
  input: z.infer<typeof CreateGlobalParameterSchema>
): Promise<any> {
  const command: Command = {
    name: "CreateGlobalParameter",
    payload: {
      name: input.name,
      type: input.type,
      spec: input.spec,
      isReporting: input.isReporting,
    },
  };

  try {
    const result = await executeCommand(command);
    return result;
  } catch (error: any) {
    return { error: error.message };
  }
}

export const create_global_parameter = tool(
  "create_global_parameter",
  "Creates a new global parameter in the Revit model.",
  CreateGlobalParameterSchema,
  createGlobalParameter
);
