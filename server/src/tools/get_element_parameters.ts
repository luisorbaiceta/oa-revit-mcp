import { z } from "zod";
import { tool } from "@modelcontext/tool-runtime";
import {
  executeCommand,
  Command,
} from "../utils/executeCommand";

const GetElementParametersSchema = z.object({
  elementId: z.number().describe("The ID of the element."),
});

async function getElementParameters(
  input: z.infer<typeof GetElementParametersSchema>
): Promise<any> {
  const command: Command = {
    name: "GetElementParameters",
    payload: {
      elementId: input.elementId,
    },
  };

  try {
    const result = await executeCommand(command);
    return result;
  } catch (error: any) {
    return { error: error.message };
  }
}

export const get_element_parameters = tool(
  "get_element_parameters",
  "Retrieves all parameters for a specified element in the Revit model.",
  GetElementParametersSchema,
  getElementParameters
);
