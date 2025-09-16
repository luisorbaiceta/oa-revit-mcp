import { z } from "zod";
import { tool } from "@modelcontext/tool-runtime";
import {
  executeCommand,
  Command,
} from "../utils/executeCommand";

const AssignGlobalParameterToElementSchema = z.object({
  elementId: z.number().describe("The ID of the element."),
  parameterName: z
    .string()
    .describe("The name of the parameter on the element."),
  globalParameterId: z
    .number()
    .describe("The ID of the global parameter to associate with."),
});

async function assignGlobalParameterToElement(
  input: z.infer<typeof AssignGlobalParameterToElementSchema>
): Promise<any> {
  const command: Command = {
    name: "AssignGlobalParameterToElement",
    payload: {
      elementId: input.elementId,
      parameterName: input.parameterName,
      globalParameterId: input.globalParameterId,
    },
  };

  try {
    const result = await executeCommand(command);
    return result;
  } catch (error: any) {
    return { error: error.message };
  }
}

export const assign_global_parameter_to_element = tool(
  "assign_global_parameter_to_element",
  "Associates a parameter of an element with a global parameter.",
  AssignGlobalParameterToElementSchema,
  assignGlobalParameterToElement
);
