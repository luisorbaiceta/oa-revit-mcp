import { z } from "zod";
import { tool } from "@modelcontext/tool-runtime";
import {
  executeCommand,
  Command,
} from "../utils/executeCommand";

const SetGlobalParameterValueSchema = z.object({
  id: z.number().describe("The ID of the global parameter."),
  value: z.any().describe("The new value for the global parameter."),
});

async function setGlobalParameterValue(
  input: z.infer<typeof SetGlobalParameterValueSchema>
): Promise<any> {
  const command: Command = {
    name: "SetGlobalParameterValue",
    payload: {
      id: input.id,
      value: input.value,
    },
  };

  try {
    const result = await executeCommand(command);
    return result;
  } catch (error: any) {
    return { error: error.message };
  }
}

export const set_global_parameter_value = tool(
  "set_global_parameter_value",
  "Sets the value of a global parameter.",
  SetGlobalParameterValueSchema,
  setGlobalParameterValue
);
