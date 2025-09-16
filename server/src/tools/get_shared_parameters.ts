import { z } from "zod";
import { tool } from "@modelcontext/tool-runtime";
import {
  executeCommand,
  Command,
} from "../utils/executeCommand";

const GetSharedParametersSchema = z.object({});

async function getSharedParameters(
  input: z.infer<typeof GetSharedParametersSchema>
): Promise<any> {
  const command: Command = {
    name: "GetSharedParameters",
    payload: {},
  };

  try {
    const result = await executeCommand(command);
    return result;
  } catch (error: any) {
    return { error: error.message };
  }
}

export const get_shared_parameters = tool(
  "get_shared_parameters",
  "Retrieves all shared parameters from the Revit model.",
  GetSharedParametersSchema,
  getSharedParameters
);
