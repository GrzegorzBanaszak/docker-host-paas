import { z } from "zod";

export const createJobSchema = z.object({
  name: z.string().trim().min(1, "Enter a job name.").max(200, "Job name is too long."),
  repositoryUrl: z.string().url("Enter a valid repository URL."),
  branch: z.string().trim().optional(),
  projectPath: z
    .string()
    .trim()
    .refine(
      (value) => !value || (!value.startsWith("/") && !value.split("/").some((segment) => segment === "." || segment === "..")),
      "Enter a relative repository path without '.' or '..' segments."
    )
    .optional()
});

export type CreateJobSchema = z.infer<typeof createJobSchema>;

export const createProjectSchema = z.object({
  name: z.string().trim().min(1, "Enter a project name.").max(200, "Project name is too long."),
  repositoryUrl: z.string().url("Enter a valid repository URL."),
  defaultBranch: z.string().trim().optional(),
  defaultProjectPath: z
    .string()
    .trim()
    .refine(
      (value) => !value || (!value.startsWith("/") && !value.split("/").some((segment) => segment === "." || segment === "..")),
      "Enter a relative repository path without '.' or '..' segments."
    )
    .optional()
});

export type CreateProjectSchema = z.infer<typeof createProjectSchema>;
