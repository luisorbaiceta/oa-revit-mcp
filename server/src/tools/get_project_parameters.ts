import { z } from "zod";
import { tool } from "@modelcontext/tool-runtime";
import {
  executeCommand,
  Command,
} from "../utils/executeCommand";

const GetProjectParametersSchema = z.object({});

async function getProjectParameters(
  input: z.infer<typeof GetProjectParametersSchema>
): Promise<any> {
  const command: Command = {
    name: "GetProjectParameters",
    payload: {},
  };

  try {
    const result = await executeCommand(command);
    return result;
  } catch (error: any) {
    return { error: error.message };
  }
}

export const get_project_parameters = tool(
  "get_project_parameters",
  "Retrieves all project parameters from the Revit model.",
  GetProjectParametersSchema,
  getProjectParameters
);
