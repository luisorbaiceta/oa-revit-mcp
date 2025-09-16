import { z } from "zod";
import { tool } from "@modelcontext/tool-runtime";
import {
  executeCommand,
  Command,
} from "../utils/executeCommand";

const UpdateElementParameterSchema = z.object({
  elementId: z.number().describe("The ID of the element to update."),
  parameterName: z.string().describe("The name of the parameter to update."),
  parameterValue: z
    .any()
    .describe("The new value for the parameter."),
});

async function updateElementParameter(
  input: z.infer<typeof UpdateElementParameterSchema>
): Promise<any> {
  const command: Command = {
    name: "UpdateElementParameter",
    payload: {
      elementId: input.elementId,
      parameterName: input.parameterName,
      parameterValue: input.parameterValue,
    },
  };

  try {
    const result = await executeCommand(command);
    return result;
  } catch (error: any) {
    return { error: error.message };
  }
}

export const update_element_parameter = tool(
  "update_element_parameter",
  "Updates the value of a specified parameter for a given element.",
  UpdateElementParameterSchema,
  updateElementParameter
);
