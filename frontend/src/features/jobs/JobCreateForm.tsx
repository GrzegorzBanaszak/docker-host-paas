import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { createJobSchema, type CreateJobSchema } from "./schema";
import { useCreateJob } from "./hooks";

export function JobCreateForm() {
  const createJob = useCreateJob();
  const form = useForm<CreateJobSchema>({
    resolver: zodResolver(createJobSchema),
    defaultValues: {
      repositoryUrl: "",
      branch: ""
    }
  });

  const onSubmit = form.handleSubmit(async (values) => {
    await createJob.mutateAsync(values);
    form.reset();
  });

  return (
    <form className="space-y-4" onSubmit={onSubmit}>
      <div>
        <label className="mb-2 block text-sm font-medium text-ink" htmlFor="repositoryUrl">
          GitHub repository URL
        </label>
        <input
          id="repositoryUrl"
          type="url"
          placeholder="https://github.com/owner/repo"
          className="w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm text-ink outline-none transition focus:border-sky focus:ring-2 focus:ring-sky/20"
          {...form.register("repositoryUrl")}
        />
        {form.formState.errors.repositoryUrl ? (
          <p className="mt-2 text-xs font-medium text-rose">{form.formState.errors.repositoryUrl.message}</p>
        ) : null}
      </div>

      <div>
        <label className="mb-2 block text-sm font-medium text-ink" htmlFor="branch">
          Branch
        </label>
        <input
          id="branch"
          type="text"
          placeholder="main"
          className="w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm text-ink outline-none transition focus:border-sky focus:ring-2 focus:ring-sky/20"
          {...form.register("branch")}
        />
      </div>

      {createJob.isError ? (
        <p className="text-sm font-medium text-rose">
          {(createJob.error as Error).message || "Could not create job."}
        </p>
      ) : null}

      <button
        type="submit"
        disabled={createJob.isPending}
        className="rounded-full bg-ink px-5 py-3 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
      >
        {createJob.isPending ? "Submitting..." : "Create job"}
      </button>
    </form>
  );
}
