import { z } from "zod";

export const createJobSchema = z.object({
  repositoryUrl: z.string().url("Enter a valid repository URL."),
  branch: z.string().trim().optional()
});

export type CreateJobSchema = z.infer<typeof createJobSchema>;
