import { z } from "zod";

export const createJobSchema = z.object({
  name: z.string().trim().min(1, "Enter a job name.").max(200, "Job name is too long."),
  repositoryUrl: z.string().url("Enter a valid repository URL."),
  branch: z.string().trim().optional()
});

export type CreateJobSchema = z.infer<typeof createJobSchema>;
