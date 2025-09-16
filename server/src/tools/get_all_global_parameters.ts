import { z } from "zod";
import { tool } from "@modelcontext/tool-runtime";
import {
  executeCommand,
  Command,
} from "../utils/executeCommand";

const GetAllGlobalParametersSchema = z.object({});

async function getAllGlobalParameters(
  input: z.infer<typeof GetAllGlobalParametersSchema>
): Promise<any> {
  const command: Command = {
    name: "GetAllGlobalParameters",
    payload: {},
  };

  try {
    const result = await executeCommand(command);
    return result;
  } catch (error: any) {
    return { error: error.message };
  }
}

export const get_all_global_parameters = tool(
  "get_all_global_parameters",
  "Retrieves all global parameters from the Revit model.",
  GetAllGlobalParametersSchema,
  getAllGlobalParameters
);
